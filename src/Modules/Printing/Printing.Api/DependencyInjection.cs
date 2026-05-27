using Microsoft.Extensions.DependencyInjection;

using Printing.Infrastructure;

namespace Printing.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPrintingApi(this IServiceCollection services)
    {
        services.AddPrintingInfrastructure();
        return services;
    }
}
