using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Templates.Infrastructure.Persistence;

internal sealed class TemplatesDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TemplatesDbContext>
{
    public TemplatesDbContext CreateDbContext(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString =
            config.GetConnectionString("TemplatesDb")
            ?? "Host=localhost;Port=5432;Database=TemplatesDb;Username=dev;Password=dev@2025";

        DbContextOptionsBuilder<TemplatesDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "templates"));

        return new TemplatesDbContext(optionsBuilder.Options);
    }
}
