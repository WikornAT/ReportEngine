using Microsoft.Extensions.DependencyInjection;

namespace Scheduling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}