using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Reporting.Application;
using Reporting.Infrastructure;

namespace Reporting.Api;

/// <summary>
/// Entry point for registering all Reporting module services into the DI container.
/// Called once from the application host's <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Reporting module application, infrastructure, and API-layer services.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configuration">The host's configuration (connection strings, options).</param>
    public static IServiceCollection AddReportingApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReportingInfrastructure(configuration);
        services.AddReportingApplication();
        return services;
    }
}
