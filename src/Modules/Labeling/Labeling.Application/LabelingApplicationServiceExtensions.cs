using Labeling.Application.GuaranteeInfo;
using Microsoft.Extensions.DependencyInjection;

namespace Labeling.Application;

/// <summary>
/// Registers all Labeling module application services into the DI container.
/// </summary>
public static class LabelingApplicationServiceExtensions
{
    /// <summary>
    /// Adds Labeling application services (e.g., <see cref="IGuaranteeInfoService"/>).
    /// Call this from your composition root after adding infrastructure services.
    /// </summary>
    public static IServiceCollection AddLabelingApplication(this IServiceCollection services)
    {
        services.AddScoped<IGuaranteeInfoService, GuaranteeInfoService>();
        return services;
    }
}
