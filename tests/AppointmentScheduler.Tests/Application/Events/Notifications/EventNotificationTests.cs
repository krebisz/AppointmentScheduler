using AppointmentScheduler.Application.Events.Persistence;
using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Application.Events.Cancel;
using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Application.Events.Notifications;
using AppointmentScheduler.Application.Events.Update;
using AppointmentScheduler.Domain.Events;
using Xunit;

namespace AppointmentScheduler.Tests.Application.Events.Notifications;

public sealed class EventNotificationTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_publishes_to_attendees_after_persistence()
    {
        var calls = new List<string>();
        var repository = new RecordingEventRepository(calls);
        var publisher = new RecordingPublisher(calls);
        var handler = new CreateEventHandler(repository, publisher);

        var result = await handler.HandleAsync(
            new CreateEventCommand(
                "Consultation",
                "Annual review",
                StartTime,
                StartTime.AddMinutes(30),
                [new CreateAttendeeCommand("Alex Patient", "alex@example.com", false)]),
            CancellationToken.None);

        Assert.Equal(["save", "publish"], calls);
        var notification = Assert.Single(publisher.Notifications);
        Assert.Equal(EventNotificationType.Created, notification.Type);
        Assert.Equal(result.Id, notification.EventId);
        Assert.Equal(["alex@example.com"], notification.RecipientEmailAddresses);
    }

    [Fact]
    public async Task Update_publishes_updated_details_and_recipients_after_persistence()
    {
        var calls = new List<string>();
        var calendarEvent = CreateEvent();
        var repository = new RecordingEventRepository(calls, calendarEvent);
        var publisher = new RecordingPublisher(calls);
        var handler = new UpdateEventHandler(repository, publisher);

        await handler.HandleAsync(
            new UpdateEventCommand(
                calendarEvent.Id,
                calendarEvent.Version,
                "Updated consultation",
                "Updated description",
                StartTime.AddHours(1),
                StartTime.AddHours(2),
                [new UpdateAttendeeCommand("New Patient", "new@example.com", true)]),
            CancellationToken.None);

        Assert.Equal(["save", "publish"], calls);
        var notification = Assert.Single(publisher.Notifications);
        Assert.Equal(EventNotificationType.Updated, notification.Type);
        Assert.Equal("Updated consultation", notification.Title);
        Assert.Equal(["new@example.com"], notification.RecipientEmailAddresses);
    }

    [Fact]
    public async Task Cancel_publishes_only_for_the_first_state_transition()
    {
        var calls = new List<string>();
        var calendarEvent = CreateEvent();
        var repository = new RecordingEventRepository(calls, calendarEvent);
        var publisher = new RecordingPublisher(calls);
        var handler = new CancelEventHandler(repository, publisher);

        await handler.HandleAsync(new CancelEventCommand(calendarEvent.Id), CancellationToken.None);
        await handler.HandleAsync(new CancelEventCommand(calendarEvent.Id), CancellationToken.None);

        Assert.Equal(["save", "publish"], calls);
        var notification = Assert.Single(publisher.Notifications);
        Assert.Equal(EventNotificationType.Cancelled, notification.Type);
    }

    private static CalendarEvent CreateEvent()
    {
        return CalendarEvent.Create(
            "Consultation",
            "Annual review",
            StartTime,
            StartTime.AddMinutes(30),
            [new AttendeeDetails("Alex Patient", "alex@example.com", false)]);
    }

    private sealed class RecordingPublisher(List<string> calls)
        : IEventNotificationPublisher
    {
        public List<EventNotification> Notifications { get; } = [];

        public Task PublishAsync(
            EventNotification notification,
            CancellationToken cancellationToken)
        {
            calls.Add("publish");

            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEventRepository(
        List<string> calls,
        CalendarEvent? calendarEvent = null) : IEventRepository
    {
        public CalendarEvent? CalendarEvent { get; private set; } = calendarEvent;

        public Task AddAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken)
        {
            CalendarEvent = calendarEvent;
            calls.Add("save");
            return Task.CompletedTask;
        }

        public Task<CalendarEvent?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                CalendarEvent?.Id == id ? CalendarEvent : null);
        }

        public Task<IReadOnlyList<CalendarEvent>> ListAsync(
            EventRepositoryQuery query,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            calls.Add("save");
            return Task.CompletedTask;
        }
    }
}
