using System.Text.RegularExpressions;

using Reporting.Domain.Enums;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Validates that stored <see cref="ReportDataSourceType.SqlQuery"/> text is
/// SELECT-only and does not contain dangerous DDL/DML statements.
/// <para>
/// <b>Design note:</b> This is a best-effort defence-in-depth guard against
/// accidental or malicious stored queries. It does <em>not</em> replace proper
/// database-level security (least-privilege roles, row-level security, etc.).
/// </para>
/// <para>
/// Only <see cref="ReportDataSourceType.SqlQuery"/> sources are validated.
/// <see cref="ReportDataSourceType.StoredProcedure"/> sources are executed by name only —
/// parameters are always bound; the procedure body is never inspected here.
/// </para>
/// </summary>
internal static class SqlSafetyValidator
{
    // Tokens that are never allowed in a SqlQuery source, regardless of position.
    private static readonly string[] _blockedKeywords =
    [
        "DROP", "DELETE", "UPDATE", "INSERT", "ALTER", "TRUNCATE",
        "CREATE", "REPLACE", "GRANT", "REVOKE", "VACUUM", "REINDEX",
        "COPY", "CALL", "DO", "EXECUTE",
    ];

    // Patterns for dynamic SQL / code execution even within a SELECT
    private static readonly Regex[] _blockedPatterns =
    [
        // EXEC / EXECUTE keyword (dynamic SQL)
        new(@"\bEXEC(UTE)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        // pg_sleep / dblink / dblink_exec style attacks
        new(@"\bpg_sleep\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bdblink\b",   RegexOptions.IgnoreCase | RegexOptions.Compiled),
        // Semicolons that might indicate statement stacking
        new(@";",            RegexOptions.Compiled),
        // Dollar-quoting (anonymous blocks / DO $$ ... $$)
        new(@"\$\$",         RegexOptions.Compiled),
    ];

    /// <summary>
    /// Checks <paramref name="queryText"/> for prohibited SQL keywords and patterns.
    /// </summary>
    /// <param name="queryText">The SQL query stored in <c>ReportDataSource.QueryText</c>.</param>
    /// <param name="sourceName">Data source name used in error messages.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a prohibited keyword or pattern is detected.
    /// </exception>
    public static void Validate(string queryText, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            throw new InvalidOperationException(
                $"Data source '{sourceName}': QueryText must not be empty.");
        }

        // Strip single-line and block comments before analysis
        string stripped = StripComments(queryText);

        foreach (string keyword in _blockedKeywords)
        {
            var pattern = new Regex(
                $@"\b{Regex.Escape(keyword)}\b",
                RegexOptions.IgnoreCase);

            if (pattern.IsMatch(stripped))
            {
                throw new InvalidOperationException(
                    $"Data source '{sourceName}': QueryText contains prohibited keyword '{keyword}'. " +
                    "Only SELECT statements are allowed for SqlQuery data sources.");
            }
        }

        foreach (Regex pattern in _blockedPatterns)
        {
            if (pattern.IsMatch(stripped))
            {
                throw new InvalidOperationException(
                    $"Data source '{sourceName}': QueryText contains a prohibited pattern. " +
                    "Only SELECT statements are allowed for SqlQuery data sources.");
            }
        }

        // Must begin with SELECT (after optional leading whitespace)
        if (!Regex.IsMatch(stripped.TrimStart(), @"^SELECT\b", RegexOptions.IgnoreCase))
        {
            throw new InvalidOperationException(
                $"Data source '{sourceName}': QueryText must begin with SELECT. " +
                "Only SELECT statements are allowed for SqlQuery data sources.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string StripComments(string sql)
    {
        // Remove /* block comments */
        string noBlock = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        // Remove -- line comments
        string noLine = Regex.Replace(noBlock, @"--[^\r\n]*", " ");
        return noLine;
    }
}
