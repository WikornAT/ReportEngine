using Microsoft.Extensions.DependencyInjection;

using Templates.Infrastructure;

namespace Templates.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddTemplatesApi(this IServiceCollection services)
    {
        services.AddTemplatesInfrastructure();
        return services;
    }
}
