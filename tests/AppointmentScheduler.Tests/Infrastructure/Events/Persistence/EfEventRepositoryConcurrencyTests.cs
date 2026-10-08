using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Infrastructure.Events.Persistence;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppointmentScheduler.Tests.Infrastructure.Events.Persistence;

public sealed class EfEventRepositoryConcurrencyTests
{
    [Fact]
    public async Task Save_rejects_the_second_writer_loaded_at_the_same_versionAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(connection)
            .Options;

        var calendarEvent = CalendarEvent.Create(
            "Consultation",
            "Annual review",
            new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 1, 9, 30, 0, TimeSpan.Zero),
            [new AttendeeDetails("Alex Patient", "alex@example.com", false)]);

        await using (var setupContext = new SchedulerDbContext(options))
        {
            await SchedulerDatabaseInitializer.InitializeAsync(setupContext);
            await new EfEventRepository(setupContext)
                .AddAndSaveAsync(calendarEvent, CancellationToken.None);
        }

        await using var firstContext = new SchedulerDbContext(options);
        await using var secondContext = new SchedulerDbContext(options);
        var firstRepository = new EfEventRepository(firstContext);
        var secondRepository = new EfEventRepository(secondContext);
        var firstWriter = await firstRepository.GetByIdAsync(
            calendarEvent.Id,
            CancellationToken.None);
        var secondWriter = await secondRepository.GetByIdAsync(
            calendarEvent.Id,
            CancellationToken.None);

        Assert.NotNull(firstWriter);
        Assert.NotNull(secondWriter);
        firstWriter.Cancel();
        secondWriter.Cancel();

        await firstRepository.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<EventConcurrencyException>(() =>
            secondRepository.SaveChangesAsync(CancellationToken.None));
    }
}
