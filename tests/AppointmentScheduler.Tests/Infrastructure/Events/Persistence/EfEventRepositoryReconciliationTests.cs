using System.Data.Common;
using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Application.Events.Notifications;
using AppointmentScheduler.Application.Events.Update;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Infrastructure.Events.Persistence;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace AppointmentScheduler.Tests.Infrastructure.Events.Persistence;

public sealed class EfEventRepositoryReconciliationTests
{
    [Fact]
    public async Task Email_reconciliation_rolls_back_when_the_aggregate_version_conflictsAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SchedulerDbContext>().UseSqlite(connection).Options;
        var eventId = await SeedAsync(options);
        await using var firstContext = new SchedulerDbContext(options);
        await using var secondContext = new SchedulerDbContext(options);
        var firstRepository = new EfEventRepository(firstContext);
        var secondRepository = new EfEventRepository(secondContext);
        var first = (await firstRepository.GetByIdAsync(eventId, CancellationToken.None))!;
        var second = (await secondRepository.GetByIdAsync(eventId, CancellationToken.None))!;
        first.Update("First writer", "Test", first.StartTime, first.EndTime,
            [new AttendeeUpdateDetails("Alex", "first@example.com", AttendeeId: first.Attendees.Single().Id)]);
        second.Update("Second writer", "Test", second.StartTime, second.EndTime,
            [new AttendeeUpdateDetails("Alex", "second@example.com", AttendeeId: second.Attendees.Single().Id)]);
        await firstRepository.SaveChangesAsync(CancellationToken.None);
        await Assert.ThrowsAsync<EventConcurrencyException>(() => secondRepository.SaveChangesAsync(CancellationToken.None));
        await using var verification = new SchedulerDbContext(options);
        var stored = await verification.Events.Include(calendarEvent => calendarEvent.Attendees).SingleAsync();
        Assert.Equal("First writer", stored.Title);
        Assert.Equal(2, stored.Version);
        Assert.Equal("first@example.com", Assert.Single(stored.Attendees).EmailAddress);
        Assert.Equal("second@example.com", second.Attendees.Single().EmailAddress);
    }

    [Fact]
    public async Task Failure_in_final_email_phase_rolls_back_event_children_and_version_without_notificationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SchedulerDbContext>().UseSqlite(connection).Options;
        var eventId = await SeedAsync(options);
        var faultyOptions = new DbContextOptionsBuilder<SchedulerDbContext>().UseSqlite(connection)
            .AddInterceptors(new FailFinalEmailInterceptor()).Options;
        await using var faultyContext = new SchedulerDbContext(faultyOptions);
        var publisher = new RecordingPublisher();
        var repository = new EfEventRepository(faultyContext);
        var original = (await repository.GetByIdAsync(eventId, CancellationToken.None))!;
        var attendeeId = original.Attendees.Single().Id;
        var handler = new UpdateEventHandler(repository, publisher);
        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => handler.HandleAsync(new UpdateEventCommand(
            eventId, 1, "Would change", "Would change description", original.StartTime, original.EndTime,
            [new UpdateAttendeeInput("Renamed", "updated@example.com", false, attendeeId),
             new UpdateAttendeeInput("Addition", "new@example.com")]), CancellationToken.None));
        Assert.IsType<InvalidOperationException>(failure.InnerException);
        Assert.Equal(0, publisher.Calls);
        await using var verification = new SchedulerDbContext(options);
        var stored = await verification.Events.Include(calendarEvent => calendarEvent.Attendees).SingleAsync();
        Assert.Equal("Original", stored.Title);
        Assert.Equal("Test", stored.Description);
        Assert.Equal(1, stored.Version);
        var retained = Assert.Single(stored.Attendees);
        Assert.Equal(attendeeId, retained.Id);
        Assert.Equal("alex@example.com", retained.EmailAddress);
        Assert.True(retained.IsAttending);
        Assert.Equal("updated@example.com", original.Attendees.Single(attendee => attendee.Id == attendeeId).EmailAddress);
    }

    private static async Task<Guid> SeedAsync(DbContextOptions<SchedulerDbContext> options)
    {
        await using var context = new SchedulerDbContext(options);
        await SchedulerDatabaseInitializer.InitializeAsync(context);
        var calendarEvent = CalendarEvent.Create("Original", "Test",
            DateTimeOffset.Parse("2077-01-01T09:00:00Z"), DateTimeOffset.Parse("2077-01-01T10:00:00Z"),
            [new AttendeeDetails("Alex", "alex@example.com", true)]);
        await new EfEventRepository(context).AddAndSaveAsync(calendarEvent, CancellationToken.None);
        return calendarEvent.Id;
    }

    private sealed class FailFinalEmailInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE \"Attendees\"", StringComparison.Ordinal)
                && command.Parameters.Cast<DbParameter>().Any(parameter => Equals(parameter.Value, "updated@example.com")))
                throw new InvalidOperationException("Injected final-email failure.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingPublisher : IEventNotificationPublisher
    {
        public int Calls { get; private set; }
        public Task PublishAsync(EventNotification notification, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
