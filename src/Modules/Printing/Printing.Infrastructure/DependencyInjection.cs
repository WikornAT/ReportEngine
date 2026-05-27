using Microsoft.Extensions.DependencyInjection;

namespace Printing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPrintingInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}