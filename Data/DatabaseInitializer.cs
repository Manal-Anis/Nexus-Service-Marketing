using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Data;

/// <summary>
/// Creates the database, applies the migrations and inserts the demo data.
/// The application calls this during startup so that a fresh machine only needs "dotnet run".
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Applies the pending migrations and seeds the demo data when the database is empty.
    /// </summary>
    /// <param name="services">Root service provider of the application.</param>
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        try
        {
            logger.LogInformation("Applying the database migrations...");

            // MigrateAsync creates the database when it does not exist yet.
            await context.Database.MigrateAsync();

            logger.LogInformation("Migrations applied. Seeding the demo data...");
            await DemoDataSeeder.SeedAsync(provider);

            logger.LogInformation("The database is ready.");
        }
        catch (Exception ex)
        {
            // The application must not start with a broken database, but the message has to be clear.
            logger.LogError(ex, "The database could not be prepared. Please check the connection string.");
            throw;
        }
    }
}
