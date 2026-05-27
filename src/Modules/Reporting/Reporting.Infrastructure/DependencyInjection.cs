using Exim.ReportEngine.SharedKernel;

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

        services.AddScoped<IReportingDbContext>(sp =>
            sp.GetRequiredService<ReportingDbContext>());

        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        services.AddScoped<IReportQueryExecutor, NotImplementedReportQueryExecutor>();
        services.AddScoped<IReportRenderer, NotImplementedReportRenderer>();
        services.AddScoped<IReportOutputStorage, NotImplementedReportStorageService>();

        return services;
    }
}
