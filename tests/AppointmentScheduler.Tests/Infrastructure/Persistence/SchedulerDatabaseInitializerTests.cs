using System.Security.Cryptography;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppointmentScheduler.Tests.Infrastructure.Persistence;

public sealed class SchedulerDatabaseInitializerTests
{
    [Fact]
    public async Task Migrations_create_fresh_schema_and_preserve_data_on_repeat_initialisationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SchedulerDbContext>().UseSqlite(connection).Options;
        await using (var context = new SchedulerDbContext(options))
        {
            await SchedulerDatabaseInitializer.InitializeAsync(context);
            var calendarEvent = CreateEvent();
            context.Events.Add(calendarEvent);
            await context.SaveChangesAsync();
        }
        await using var reopened = new SchedulerDbContext(options);
        await SchedulerDatabaseInitializer.InitializeAsync(reopened);
        Assert.Single(await reopened.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await reopened.Database.GetPendingMigrationsAsync());
        Assert.False(reopened.Database.HasPendingModelChanges());
        var stored = Assert.Single(await reopened.Events.Include(calendarEvent => calendarEvent.Attendees).ToListAsync());
        Assert.Equal("Migration test", stored.Title);
        Assert.Equal("alex@example.com", Assert.Single(stored.Attendees).EmailAddress);
    }

    [Fact]
    public async Task Legacy_database_copy_is_blocked_before_mutation_or_history_creationAsync()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"scheduler-legacy-{Guid.NewGuid():N}.db");
        var copyPath = Path.Combine(Path.GetTempPath(), $"scheduler-legacy-copy-{Guid.NewGuid():N}.db");
        try
        {
            await using (var legacy = new SchedulerDbContext(Options(sourcePath)))
            {
                // Deliberately simulate the old schema-creation process solely to test transition refusal.
                await legacy.Database.EnsureCreatedAsync();
                legacy.Events.Add(CreateEvent());
                await legacy.SaveChangesAsync();
            }
            File.Copy(sourcePath, copyPath);
            var before = SHA256.HashData(await File.ReadAllBytesAsync(copyPath));
            await using (var copy = new SchedulerDbContext(Options(copyPath)))
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    SchedulerDatabaseInitializer.InitializeAsync(copy));
                Assert.Contains("without migration history", exception.Message);
                Assert.Empty(await copy.Database.GetAppliedMigrationsAsync());
                Assert.Equal(1, await copy.Events.CountAsync());
            }
            Assert.Equal(before, SHA256.HashData(await File.ReadAllBytesAsync(copyPath)));
            Assert.Equal(before, SHA256.HashData(await File.ReadAllBytesAsync(sourcePath)));
        }
        finally
        {
            File.Delete(copyPath);
            File.Delete(sourcePath);
        }
    }

    private static DbContextOptions<SchedulerDbContext> Options(string path) =>
        new DbContextOptionsBuilder<SchedulerDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;

    private static CalendarEvent CreateEvent() => CalendarEvent.Create(
        "Migration test", "Test", DateTimeOffset.Parse("2077-01-01T09:00:00Z"),
        DateTimeOffset.Parse("2077-01-01T10:00:00Z"),
        [new AttendeeDetails("Alex", "alex@example.com", false)]);
}
