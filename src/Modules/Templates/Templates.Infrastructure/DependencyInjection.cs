using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ReportEngine.Contracts.Templates;

using Templates.Application.Contracts;
using Templates.Infrastructure.Catalog;
using Templates.Infrastructure.Options;
using Templates.Infrastructure.Persistence;
using Templates.Infrastructure.Repositories;
using Templates.Infrastructure.Services;

namespace Templates.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTemplatesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TemplatesDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("TemplatesDb"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "templates")));

        services.AddScoped<ITemplatesDbContext>(sp =>
            sp.GetRequiredService<TemplatesDbContext>());

        services.AddScoped<IReportTemplateRepository, ReportTemplateRepository>();

        // ── Import pipeline services ──────────────────────────────────────
        services.Configure<TemplateStorageOptions>(opts =>
            configuration.GetSection(TemplateStorageOptions.SectionName).Bind(opts));

        services.AddScoped<ITemplateStorageService, TemplateAssetStorageService>();
        services.AddScoped<ITemplateHtmlSanitizer, TemplateHtmlSanitizer>();
        services.AddScoped<ITemplateAssetReferenceRewriter, TemplateAssetReferenceRewriter>();

        // ── Cross-module catalog ──────────────────────────────────────────
        services.AddScoped<ITemplateCatalog, TemplateCatalog>();

        return services;
    }
}

