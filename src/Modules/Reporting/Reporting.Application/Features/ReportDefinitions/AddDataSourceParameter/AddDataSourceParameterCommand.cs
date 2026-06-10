using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.AddDataSourceParameter;

/// <summary>
/// Adds a parameter mapping to a <see cref="Domain.ReportDefinitions.ReportDataSource"/>.
/// Maps a SQL/SP parameter name to a declared report parameter.
/// </summary>
/// <param name="ReportDefinitionId">The owning report definition.</param>
/// <param name="DataSourceId">The target data source.</param>
/// <param name="SourceParameterName">Parameter name in the SQL query / stored procedure (e.g. <c>start_date</c>).</param>
/// <param name="ReportParameterName">Name of the declared report parameter to bind (e.g. <c>startDate</c>).</param>
/// <param name="DbType">Optional ADO.NET DB type hint (e.g. <c>Date</c>, <c>Int32</c>). Null = inferred.</param>
/// <param name="IsRequired">Whether execution should fail when no value is resolved.</param>
/// <param name="DefaultValue">Optional fallback value when the report parameter is absent.</param>
public sealed record AddDataSourceParameterCommand(
    Guid ReportDefinitionId,
    Guid DataSourceId,
    string SourceParameterName,
    string ReportParameterName,
    string? DbType,
    bool IsRequired,
    string? DefaultValue) : IRequest<Result<ReportDataSourceDto>>;
