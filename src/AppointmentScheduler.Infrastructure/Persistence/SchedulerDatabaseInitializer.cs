using System.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Infrastructure.Persistence;

public static class SchedulerDatabaseInitializer
{
    public static async Task InitializeAsync(
        SchedulerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // Use migrations for schema creation/upgrades, refusing legacy tables without migration history.
        var connection = dbContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*) FROM sqlite_master
                WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
                AND name NOT IN ('__EFMigrationsHistory', '__EFMigrationsLock');
                """;
            var tableCount = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            if (tableCount > 0)
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";
                var hasHistory = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
                var migrationCount = 0L;
                if (hasHistory)
                {
                    command.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";
                    migrationCount = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
                }
                if (migrationCount == 0)
                {
                    throw new InvalidOperationException(
                        "The database contains existing tables without migration history. " +
                        "Automatic migration of an EnsureCreated or unrecognised database is blocked. " +
                        "Use a separate fresh database, or perform a reviewed baseline transition on a backup; " +
                        "existing data has not been reset or migrated.");
                }
            }
        }
        finally
        {
            if (!wasOpen) await connection.CloseAsync();
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
