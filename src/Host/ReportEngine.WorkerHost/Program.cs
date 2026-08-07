using Quartz;

using ReportEngine.WorkerHost;
using ReportEngine.WorkerHost.Scheduling;

using Reporting.Application;
using Reporting.Infrastructure;

using Templates.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.AddReportingInfrastructure(builder.Configuration);
builder.Services.AddReportingApplication();
builder.Services.AddTemplatesInfrastructure(builder.Configuration);

builder.Services.Configure<WorkerSchedulingOptions>(
    builder.Configuration.GetSection(WorkerSchedulingOptions.SectionName));

builder.Services.AddQuartz(options =>
{
    const string defaultCron = "0 0 8 * * ?";
    const string defaultTimeZone = "SE Asia Standard Time";

    string cron = builder.Configuration[$"{WorkerSchedulingOptions.SectionName}:DailyCron"] ?? defaultCron;
    string timeZoneId = builder.Configuration[$"{WorkerSchedulingOptions.SectionName}:TimeZoneId"] ?? defaultTimeZone;

    JobKey jobKey = new("scheduled-report-execution", "reporting");
    options.AddJob<ScheduledReportExecutionJob>(job => job.WithIdentity(jobKey));
    options.AddTrigger(trigger => trigger
        .WithIdentity("scheduled-report-execution-trigger", "reporting")
        .ForJob(jobKey)
        .WithCronSchedule(cron, cronOptions =>
            cronOptions.InTimeZone(ResolveTimeZoneOrUtc(timeZoneId))));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var host = builder.Build();
host.Run();

static TimeZoneInfo ResolveTimeZoneOrUtc(string timeZoneId)
{
    try
    {
        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
    catch (TimeZoneNotFoundException)
    {
        return TimeZoneInfo.Utc;
    }
    catch (InvalidTimeZoneException)
    {
        return TimeZoneInfo.Utc;
    }
}
