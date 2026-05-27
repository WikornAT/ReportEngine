using System.Reflection;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Reporting.Application.Behaviours;

namespace Reporting.Application;

/// <summary>
/// Registers all Reporting module application-layer services into the DI container.
/// Called once from the module's API entry point (<c>AddReportingApi</c>).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds MediatR handlers, FluentValidation validators, and pipeline behaviours
    /// for the Reporting application layer.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddReportingApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        // ── MediatR ───────────────────────────────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Validation runs before every handler — no need to decorate handlers.
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehaviour<,>));
        });

        // ── FluentValidation ──────────────────────────────────────────────────
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
