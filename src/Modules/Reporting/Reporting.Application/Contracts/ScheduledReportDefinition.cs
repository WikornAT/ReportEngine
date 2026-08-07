using Reporting.Domain.Enums;

namespace Reporting.Application.Contracts;

public sealed record ScheduledReportDefinition(
    Guid Id,
    Guid ReportDefinitionId,
    ReportScheduleType ScheduleType,
    string ParametersJson,
    IReadOnlyList<ReportOutputFormat> RequestedFormats,
    string TriggeredBy);
