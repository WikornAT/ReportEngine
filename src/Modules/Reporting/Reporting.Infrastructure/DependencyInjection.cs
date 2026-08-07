
using ReportEngine.SharedKernel;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Reporting.Application.Contracts;
using Reporting.Infrastructure.Persistence;
using Reporting.Infrastructure.Services;

namespace Reporting.Infrastructure;

/// <summary>
/// Registers all Reporting infrastructure services (EF Core, repositories, etc.).
/// Extended as the infrastructure layer grows.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Reporting infrastructure services.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">The host's configuration.</param>
    public static IServiceCollection AddReportingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ReportingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("ReportingDb"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "reporting")));

        services.AddDbContextFactory<ReportingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("ReportingDb"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "reporting")),
            ServiceLifetime.Scoped);

        services.AddScoped<IReportingDbContext>(sp =>
            sp.GetRequiredService<ReportingDbContext>());

        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IReportQueryExecutor, PostgreSqlReportQueryExecutor>();
        services.AddScoped<IReportRenderer, HtmlReportRenderer>();
        services.AddScoped<IReportOutputStorage, LocalFileReportOutputStorage>();
        services.AddScoped<IReportScheduleProvider, DbReportScheduleProvider>();
        services.AddSingleton<IHtmlToPdfRenderer, PlaywrightHtmlToPdfRenderer>();
        services.AddScoped<ITemplateBindingEngine, ScribanTemplateBindingEngine>();
        services.Configure<HtmlRendererOptions>(
            configuration.GetSection(HtmlRendererOptions.SectionName));
        services.Configure<ReportOutputStorageOptions>(
            configuration.GetSection(ReportOutputStorageOptions.SectionName));

        services.AddScoped<ITemplateVerifier, TemplateVerifier>();

        services.AddScoped<IBatchReportOrchestrator, BatchReportOrchestrator>();
        services.Configure<BatchRenderOptions>(
            configuration.GetSection(BatchRenderOptions.SectionName));

        return services;
    }
}
