using Microsoft.Extensions.DependencyInjection;

using Scheduling.Infrastructure;

namespace Scheduling.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingApi(this IServiceCollection services)
    {
        services.AddSchedulingInfrastructure();
        return services;
    }
}
