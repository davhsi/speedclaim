using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SpeedClaim.Api.Context;

/// <summary>
/// Keeps EF design-time commands independent from application startup and external configuration
/// providers. Runtime dependency injection continues to construct the context in Program.cs.
/// </summary>
public sealed class SpeedClaimDbContextFactory : IDesignTimeDbContextFactory<SpeedClaimDbContext>
{
    public SpeedClaimDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = "Host=localhost;Database=speedclaim;Username=postgres";

        var options = new DbContextOptionsBuilder<SpeedClaimDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new SpeedClaimDbContext(options);
    }
}
