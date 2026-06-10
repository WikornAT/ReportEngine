using MediatR;

using ReportEngine.SharedKernel;
using Reporting.Application.DTOs;

namespace Reporting.Application.Features.ReportDefinitions.UpdateDataSourceParameter;

/// <summary>
/// Updates an existing parameter mapping on a <see cref="Domain.ReportDefinitions.ReportDataSource"/>.
/// </summary>
public sealed record UpdateDataSourceParameterCommand(
    Guid ReportDefinitionId,
    Guid DataSourceId,
    Guid ParameterId,
    string SourceParameterName,
    string ReportParameterName,
    string? DbType,
    bool IsRequired,
    string? DefaultValue) : IRequest<Result<ReportDataSourceDto>>;
