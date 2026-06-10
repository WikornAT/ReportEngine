using MediatR;

using ReportEngine.SharedKernel;

namespace Reporting.Application.Features.ReportDefinitions.RemoveDataSourceParameter;

/// <summary>
/// Removes a parameter mapping from a <see cref="Domain.ReportDefinitions.ReportDataSource"/>.
/// </summary>
public sealed record RemoveDataSourceParameterCommand(
    Guid ReportDefinitionId,
    Guid DataSourceId,
    Guid ParameterId) : IRequest<Result<Unit>>;
