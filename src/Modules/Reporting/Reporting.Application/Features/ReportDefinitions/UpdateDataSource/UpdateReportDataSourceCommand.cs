using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Domain.Enums;

namespace Reporting.Application.Features.ReportDefinitions.UpdateDataSource;

/// <summary>
/// Updates all mutable fields of an existing data source on a <see cref="Domain.ReportDefinitions.ReportDefinition"/>.
/// </summary>
/// <param name="ReportDefinitionId">The owning report definition.</param>
/// <param name="DataSourceId">The data source to update.</param>
/// <param name="Name">New logical data source name (unique within the report).</param>
/// <param name="DataSourceType">New connection/query technology.</param>
/// <param name="ConnectionStringName">New named connection string reference (no literal strings).</param>
/// <param name="QueryText">New SQL, SP name, endpoint path, or selector.</param>
/// <param name="SortOrder">New display order.</param>
public sealed record UpdateReportDataSourceCommand(
    Guid ReportDefinitionId,
    Guid DataSourceId,
    string Name,
    ReportDataSourceType DataSourceType,
    string ConnectionStringName,
    string QueryText,
    int SortOrder) : IRequest<Result<ReportDataSourceDto>>;
