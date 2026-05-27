using Dashboard.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardApi(this IServiceCollection services)
    {
        services.AddDashboardInfrastructure();
        return services;
    }
}
