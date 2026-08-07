using MediatR;

using Microsoft.Extensions.Options;

using Quartz;

using ReportEngine.WorkerHost.Scheduling;

using Reporting.Application.Contracts;
using Reporting.Application.Features.ReportExecutions.Execute;

namespace ReportEngine.WorkerHost;

[DisallowConcurrentExecution]
internal sealed class ScheduledReportExecutionJob : IJob
{
    private static readonly Action<ILogger, string, string, Exception?> _logCycleStart =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1001, nameof(ScheduledReportExecutionJob)),
            "Scheduled report cycle at {LocalTime} due types: {DueTypes}");

    private static readonly Action<ILogger, Guid, Guid, Exception?> _logExecuted =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(1002, nameof(ScheduledReportExecutionJob)),
            "Scheduled report executed. ScheduleId={ScheduleId}, ReportDefinitionId={ReportDefinitionId}");

    private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logFailed =
        LoggerMessage.Define<Guid, Guid, string>(
            LogLevel.Error,
            new EventId(1003, nameof(ScheduledReportExecutionJob)),
            "Scheduled report failed. ScheduleId={ScheduleId}, ReportDefinitionId={ReportDefinitionId}, Message={Message}");

    private static readonly Action<ILogger, string, Exception?> _logTimezoneFallback =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1004, nameof(ScheduledReportExecutionJob)),
            "Invalid TimeZoneId '{TimeZoneId}' in worker scheduling options. Falling back to UTC.");

    private readonly IReportScheduleProvider _scheduleProvider;
    private readonly IMediator _mediator;
    private readonly IOptions<WorkerSchedulingOptions> _options;
    private readonly ILogger<ScheduledReportExecutionJob> _logger;

    public ScheduledReportExecutionJob(
        IReportScheduleProvider scheduleProvider,
        IMediator mediator,
        IOptions<WorkerSchedulingOptions> options,
        ILogger<ScheduledReportExecutionJob> logger)
    {
        _scheduleProvider = scheduleProvider;
        _mediator = mediator;
        _options = options;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        DateTimeOffset localNow = ConvertToConfiguredLocalTime(nowUtc);

        IReadOnlySet<Reporting.Domain.Enums.ReportScheduleType> dueTypes =
            ScheduleCalendarPolicy.GetDueScheduleTypes(localNow);

        _logCycleStart(_logger, localNow.ToString("O"), string.Join(',', dueTypes), null);

        IReadOnlyList<ScheduledReportDefinition> schedules =
            await _scheduleProvider.GetActiveAsync(cancellationToken);

        foreach (ScheduledReportDefinition schedule in schedules.Where(s => dueTypes.Contains(s.ScheduleType)))
        {
            var command = new ExecuteReportCommand(
                ReportDefinitionId: schedule.ReportDefinitionId,
                ParametersJson: schedule.ParametersJson,
                RequestedFormats: schedule.RequestedFormats,
                CorrelationId: $"sched:{schedule.Id:N}:{nowUtc:yyyyMMddHHmmss}");

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logExecuted(_logger, schedule.Id, schedule.ReportDefinitionId, null);
            }
            else
            {
                _logFailed(_logger, schedule.Id, schedule.ReportDefinitionId, result.Error.Message, null);
            }
        }
    }

    private DateTimeOffset ConvertToConfiguredLocalTime(DateTimeOffset utcNow)
    {
        string configuredTimeZoneId = _options.Value.TimeZoneId;

        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuredTimeZoneId);
            return TimeZoneInfo.ConvertTime(utcNow, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            _logTimezoneFallback(_logger, configuredTimeZoneId, null);
            return utcNow;
        }
        catch (InvalidTimeZoneException)
        {
            _logTimezoneFallback(_logger, configuredTimeZoneId, null);
            return utcNow;
        }
    }
}
