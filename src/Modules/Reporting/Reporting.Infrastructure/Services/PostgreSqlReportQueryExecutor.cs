using System.Data;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using Reporting.Application.Contracts;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Executes <see cref="ReportDataSource"/> queries against a PostgreSQL database using
/// raw ADO.NET (Npgsql), bypassing EF Core for data retrieval so that arbitrary
/// parameterized SELECT statements and stored procedures can be run safely.
/// </summary>
/// <remarks>
/// Security contract:
/// <list type="bullet">
///   <item>SQL text is <em>always</em> taken from the stored <see cref="ReportDataSource.QueryText"/>
///         — never from API request payloads.</item>
///   <item>All runtime values are passed as named Npgsql parameters.</item>
///   <item><see cref="SqlSafetyValidator"/> is applied to every <see cref="ReportDataSourceType.SqlQuery"/>
///         source before execution.</item>
///   <item><see cref="ReportDataSourceType.StoredProcedure"/> sources invoke the stored procedure by
///         name only; no raw SQL is composed.</item>
/// </list>
/// </remarks>
internal sealed class PostgreSqlReportQueryExecutor : IReportQueryExecutor
{
    private readonly IDbContextFactory<Persistence.ReportingDbContext> _dbContextFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgreSqlReportQueryExecutor> _logger;

    private static readonly Action<ILogger, string, Exception?> _logExecuting =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(50, "DataSourceExecuting"),
            "Executing data source '{SourceName}'");

    private static readonly Action<ILogger, string, int, long, Exception?> _logExecuted =
        LoggerMessage.Define<string, int, long>(
            LogLevel.Debug,
            new EventId(51, "DataSourceExecuted"),
            "Data source '{SourceName}' returned {RowCount} rows in {ElapsedMs}ms");

    private static readonly Action<ILogger, string, Exception?> _logFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(52, "DataSourceFailed"),
            "Data source '{SourceName}' execution failed");

    public PostgreSqlReportQueryExecutor(
        IDbContextFactory<Persistence.ReportingDbContext> dbContextFactory,
        IConfiguration configuration,
        ILogger<PostgreSqlReportQueryExecutor> logger)
    {
        _dbContextFactory = dbContextFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DataSourceExecutionResult> ExecuteAsync(
        Guid reportDefinitionId,
        string parametersJson,
        CancellationToken cancellationToken = default)
    {
        long totalStart = Environment.TickCount64;

        // ── 1. Load data sources (with their parameter mappings) ──────────
        await using Persistence.ReportingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<ReportDataSource> sources = await dbContext.ReportDefinitions
            .AsNoTracking()
            .Where(d => d.Id == reportDefinitionId)
            .SelectMany(d => d.DataSources)
            .Include(ds => ds.Parameters)
            .Where(ds => ds.IsActive)
            .OrderBy(ds => ds.SortOrder)
            .ToListAsync(cancellationToken);

        // ── 2. Parse caller-supplied parameter values ─────────────────────
        Dictionary<string, JsonElement> callerParams = ParseCallerParams(parametersJson);

        // ── 3. Execute each data source ───────────────────────────────────
        var resultDict  = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var metrics     = new List<DataSourceMetric>(sources.Count);

        foreach (ReportDataSource source in sources)
        {
            long srcStart = Environment.TickCount64;
            _logExecuting(_logger, source.Name, null);

            try
            {
                List<Dictionary<string, object?>> rows = await ExecuteSourceAsync(
                    source, callerParams, cancellationToken);

                long elapsed = Environment.TickCount64 - srcStart;
                _logExecuted(_logger, source.Name, rows.Count, elapsed, null);

                // Convention: _data_{sourceName} so HtmlReportRenderer.BuildRenderContext
                // places it in the Scriban `data` namespace.
                string key = $"_data_{source.Name}";
                resultDict[key] = rows;
                metrics.Add(new DataSourceMetric(source.Name, rows.Count, elapsed, null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                long elapsed = Environment.TickCount64 - srcStart;
                _logFailed(_logger, source.Name, ex);
                metrics.Add(new DataSourceMetric(source.Name, 0, elapsed, ex.Message));
                // Surface the error to the caller; partial results are not returned
                throw new InvalidOperationException(
                    $"Data source '{source.Name}' failed: {ex.Message}", ex);
            }
        }

        // ── 4. Serialize combined result to JSON ──────────────────────────
        string dataJson = SerializeResults(resultDict);
        long totalMs = Environment.TickCount64 - totalStart;

        return new DataSourceExecutionResult(dataJson, totalMs, metrics);
    }

    // ── Private: per-source execution ─────────────────────────────────────────

    private async Task<List<Dictionary<string, object?>>> ExecuteSourceAsync(
        ReportDataSource source,
        Dictionary<string, JsonElement> callerParams,
        CancellationToken cancellationToken)
    {
        // Non-database sources (Json, InMemory, WebService) have no query to run.
        // Parameters are passed directly by the caller via parametersJson.
        if (source.DataSourceType is
            ReportDataSourceType.Json or
            ReportDataSourceType.InMemory or
            ReportDataSourceType.WebService)
        {
            return [];
        }

        string? connectionString = _configuration.GetConnectionString(source.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Data source '{source.Name}' failed: Connection string '{source.ConnectionStringName}' not found in configuration.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using NpgsqlCommand command = BuildCommand(connection, source, callerParams);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, cancellationToken);

        return await ReadRowsAsync(reader, cancellationToken);
    }

    private static NpgsqlCommand BuildCommand(
        NpgsqlConnection connection,
        ReportDataSource source,
        Dictionary<string, JsonElement> callerParams)
    {
        NpgsqlCommand command;

        if (source.DataSourceType == ReportDataSourceType.StoredProcedure)
        {
            // Validate: stored procedure name must be a simple identifier
            if (!IsValidIdentifier(source.QueryText))
            {
                throw new InvalidOperationException(
                    $"Data source '{source.Name}': StoredProcedure QueryText must be a plain " +
                    "schema-qualified identifier (e.g., 'reporting.get_invoice_data').");
            }

            command = new NpgsqlCommand(source.QueryText, connection)
            {
                CommandType = CommandType.StoredProcedure,
            };
        }
        else if (source.DataSourceType == ReportDataSourceType.SqlQuery)
        {
            SqlSafetyValidator.Validate(source.QueryText, source.Name);

            command = new NpgsqlCommand(source.QueryText, connection)
            {
                CommandType = CommandType.Text,
            };
        }
        else if (source.DataSourceType == ReportDataSourceType.Json
              || source.DataSourceType == ReportDataSourceType.InMemory
              || source.DataSourceType == ReportDataSourceType.WebService)
        {
            // Should not be reached; ExecuteSourceAsync short-circuits before calling BuildCommand.
            throw new InvalidOperationException(
                $"Data source '{source.Name}': DataSourceType '{source.DataSourceType}' " +
                "should not reach BuildCommand.");
        }
        else
        {
            throw new NotSupportedException(
                $"Data source '{source.Name}': DataSourceType '{source.DataSourceType}' " +
                "is not supported by PostgreSqlReportQueryExecutor.");
        }

        if (source.TimeoutSeconds.HasValue)
        {
            command.CommandTimeout = source.TimeoutSeconds.Value;
        }

        // Bind parameters
        foreach (ReportDataSourceParameter mapping in source.Parameters)
        {
            object? value = ResolveParameterValue(mapping, callerParams);

            NpgsqlParameter param = command.Parameters.Add(
                new NpgsqlParameter(mapping.SourceParameterName, value ?? DBNull.Value));

            if (!string.IsNullOrWhiteSpace(mapping.DbType) &&
                Enum.TryParse<NpgsqlTypes.NpgsqlDbType>(mapping.DbType, ignoreCase: true, out NpgsqlTypes.NpgsqlDbType npgsqlType))
            {
                param.NpgsqlDbType = npgsqlType;
            }
        }

        return command;
    }

    private static object? ResolveParameterValue(
        ReportDataSourceParameter mapping,
        Dictionary<string, JsonElement> callerParams)
    {
        if (callerParams.TryGetValue(mapping.ReportParameterName, out JsonElement element))
        {
            return JsonElementToClr(element);
        }

        if (!string.IsNullOrWhiteSpace(mapping.DefaultValue))
        {
            return mapping.DefaultValue;
        }

        if (mapping.IsRequired)
        {
            throw new InvalidOperationException(
                $"Required data source parameter '{mapping.SourceParameterName}' " +
                $"(mapped from report parameter '{mapping.ReportParameterName}') " +
                "has no value and no default.");
        }

        return null;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                object? colValue = await reader.IsDBNullAsync(i, cancellationToken)
                    ? null
                    : reader.GetValue(i);

                row[colName] = NormalizeValue(colValue);
            }

            rows.Add(row);
        }

        return rows;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, JsonElement> ParseCallerParams(string parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson) || parametersJson.Trim() == "{}")
        {
            return [];
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(parametersJson);

            // Array payload (multi-page): use the first element to resolve query parameters.
            // All pages share the same parameter schema so the data source executes once.
            JsonElement root = doc.RootElement.ValueKind == JsonValueKind.Array
                && doc.RootElement.GetArrayLength() > 0
                ? doc.RootElement[0].Clone()
                : doc.RootElement;

            var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty prop in root.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }

            return dict;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
    };

    private static string SerializeResults(Dictionary<string, object?> resultDict) =>
        JsonSerializer.Serialize(resultDict, _jsonOptions);

    private static object? JsonElementToClr(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.True    => (object?)true,
            JsonValueKind.False   => false,
            JsonValueKind.Null    => null,
            JsonValueKind.Number  => element.TryGetInt64(out long l) ? l
                                   : (object?)element.GetDouble(),
            JsonValueKind.String  => element.GetString(),
            _                     => element.GetRawText(),
        };

    private static object? NormalizeValue(object? value) =>
        value switch
        {
            DBNull   => null,
            DateTime dt => (DateTimeOffset)dt,
            _        => value,
        };

    /// <summary>
    /// Allows only safe stored-procedure name formats:
    /// optional schema prefix, then an identifier — no spaces, special chars, or SQL injection.
    /// Example: <c>get_orders</c>, <c>reporting.get_orders</c>.
    /// </summary>
    private static bool IsValidIdentifier(string name) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            name, @"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)?$");
}
