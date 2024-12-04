using DrawTogether.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DrawTogether.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await EnsureCreatedAsync(dbContext, stoppingToken);
            await RunMigrationAsync(dbContext, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
        }

        hostApplicationLifetime.StopApplication();
    }

    private async Task EnsureCreatedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        var databaseCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();

        await strategy.ExecuteAsync(async ct =>
        {
            if (!await databaseCreator.ExistsAsync(ct))
            {
                await databaseCreator.CreateAsync(ct);
                logger.LogInformation("Database created");
            }
        }, cancellationToken);
    }

    private async Task RunMigrationAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            await dbContext.Database.MigrateAsync(ct);
            logger.LogInformation("Database migrated");
        }, cancellationToken);
    }
}