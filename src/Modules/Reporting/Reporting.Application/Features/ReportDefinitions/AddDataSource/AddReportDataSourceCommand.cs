using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;
using Reporting.Domain.Enums;

namespace Reporting.Application.Features.ReportDefinitions.AddDataSource;

/// <summary>
/// Binds a new data source to an existing <see cref="Domain.ReportDefinitions.ReportDefinition"/>.
/// </summary>
/// <param name="ReportDefinitionId">The owning report definition.</param>
/// <param name="Name">Logical data source name (unique within the report).</param>
/// <param name="DataSourceType">Connection/query technology.</param>
/// <param name="ConnectionStringName">Named connection string reference (no literal strings).</param>
/// <param name="QueryText">SQL, SP name, endpoint path, or selector.</param>
/// <param name="SortOrder">Display order.</param>
public sealed record AddReportDataSourceCommand(
    Guid ReportDefinitionId,
    string Name,
    ReportDataSourceType DataSourceType,
    string ConnectionStringName,
    string QueryText,
    int SortOrder) : IRequest<Result<ReportDefinitionDto>>;
