using Designer.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

namespace Designer.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDesignerApi(this IServiceCollection services)
    {
        services.AddDesignerInfrastructure();
        return services;
    }
}
