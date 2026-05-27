using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Templates.Application.Contracts;
using Templates.Infrastructure.Persistence;
using Templates.Infrastructure.Repositories;

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

        return services;
    }
}
