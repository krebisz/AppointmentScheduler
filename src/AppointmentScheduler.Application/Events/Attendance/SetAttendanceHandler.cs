using AppointmentScheduler.Application.Events.Notifications;

namespace AppointmentScheduler.Application.Events.Attendance;

public sealed class SetAttendanceHandler(
    IEventRepository eventRepository,
    IEventNotificationPublisher eventNotificationPublisher)
{
    public async Task HandleAsync(
        SetAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await eventRepository.GetByIdAsync(
            command.EventId,
            cancellationToken)
            ?? throw new EventNotFoundException(command.EventId);

        if (command.Version != calendarEvent.Version)
        {
            throw new EventConcurrencyException(
                calendarEvent.Id,
                command.Version,
                calendarEvent.Version);
        }

        var changed = calendarEvent.SetAttendance(
            command.AttendeeId,
            command.IsAttending);

        if (changed)
        {
            await eventRepository.SaveChangesAsync(cancellationToken);
            await eventNotificationPublisher.PublishAsync(
                EventNotification.From(
                    calendarEvent,
                    EventNotificationType.Updated),
                cancellationToken);
        }

    }
}

public sealed record SetAttendanceCommand(
    Guid EventId,
    Guid AttendeeId,
    bool IsAttending,
    long Version);
