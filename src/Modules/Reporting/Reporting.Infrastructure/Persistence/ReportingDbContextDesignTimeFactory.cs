using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Reporting.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> tooling to instantiate
/// <see cref="ReportingDbContext"/> without the application host.
/// </summary>
internal sealed class ReportingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString =
            config.GetConnectionString("ReportingDb")
            ?? "Host=localhost;Port=5432;Database=ReportingDb;Username=dev;Password=dev@2025";

        DbContextOptionsBuilder<ReportingDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "reporting"));

        return new ReportingDbContext(optionsBuilder.Options);
    }
}
