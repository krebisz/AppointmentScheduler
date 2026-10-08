using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Application.Events.Notifications;
using AppointmentScheduler.Infrastructure.Events.Persistence;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppointmentScheduler.Tests.Application.Events.Create;

public sealed class CreateEventHandlerTests
{
    [Fact]
    public async Task Handle_persists_event_and_owned_attendee_to_sqliteAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new SchedulerDbContext(options))
        {
            await SchedulerDatabaseInitializer.InitializeAsync(setupContext);

            var handler = new CreateEventHandler(
                new EfEventRepository(setupContext),
                new NoOpNotificationPublisher());
            var result = await handler.HandleAsync(
                new CreateEventCommand(
                    "Consultation",
                    "Annual review",
                    new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.FromHours(2)),
                    new DateTimeOffset(2026, 10, 1, 9, 30, 0, TimeSpan.FromHours(2)),
                    [new CreateAttendeeInput("Alex Patient", "alex@example.com", false)]),
                CancellationToken.None);

            Assert.NotEqual(Guid.Empty, result.Id);
        }

        await using var verificationContext = new SchedulerDbContext(options);
        var storedEvent = await verificationContext.Events
            .Include(calendarEvent => calendarEvent.Attendees)
            .SingleAsync();

        Assert.Equal("Consultation", storedEvent.Title);
        Assert.Equal(TimeSpan.Zero, storedEvent.StartTime.Offset);
        var attendee = Assert.Single(storedEvent.Attendees);
        Assert.Equal("alex@example.com", attendee.EmailAddress);
        Assert.False(attendee.IsAttending);
    }

    private sealed class NoOpNotificationPublisher : IEventNotificationPublisher
    {
        public Task PublishAsync(
            EventNotification notification,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
