using Designer.Application.Contracts;
using Designer.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Designer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDesignerInfrastructure(this IServiceCollection services)
    {
        // Singleton: catalog is built once from wwwroot/fonts and cached.
        services.AddSingleton<IFontCatalogService, FontCatalogService>();

        return services;
    }
}
