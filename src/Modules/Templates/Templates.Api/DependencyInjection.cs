using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Templates.Application;
using Templates.Infrastructure;

namespace Templates.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddTemplatesApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTemplatesInfrastructure(configuration);
        services.AddTemplatesApplication();
        return services;
    }
}

