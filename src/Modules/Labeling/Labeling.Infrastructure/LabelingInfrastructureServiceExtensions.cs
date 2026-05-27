using Labeling.Domain.GuaranteeDebt;
using Labeling.Domain.GuaranteeInfo;
using Labeling.Infrastructure.Persistence;
using Labeling.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Labeling.Infrastructure;

/// <summary>
/// Registers all Labeling module infrastructure services into the DI container.
/// Call this from your composition root (e.g., Program.cs or a module registration class).
/// </summary>
public static class LabelingInfrastructureServiceExtensions
{
    /// <summary>
    /// Adds the LabelingDbContext (PostgreSQL) and repository implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Must contain a connection string named <c>LabelingDb</c>.
    /// Example appsettings.json entry:
    /// <code>
    /// "ConnectionStrings": {
    ///   "LabelingDb": "Host=localhost;Database=t4d_dev;Username=...;Password=..."
    /// }
    /// </code>
    /// </param>
    public static IServiceCollection AddLabelingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<LabelingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LabelingDb")));

        services.AddScoped<IGuaranteeInfoRepository, GuaranteeInfoRepository>();
        services.AddScoped<IGuaranteeDebtRepository, GuaranteeDebtRepository>();

        return services;
    }
}
