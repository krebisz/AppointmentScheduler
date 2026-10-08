using AppointmentScheduler.Application.Events.Persistence;
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

        if (command.ExpectedVersion != calendarEvent.Version)
        {
            throw new EventConcurrencyException(
                calendarEvent.Id,
                command.ExpectedVersion,
                calendarEvent.Version);
        }

        var changed = calendarEvent.SetAttendance(
            command.AttendeeId,
            command.IsAttending);

        if (changed)
        {
            await eventRepository.SaveChangesAsync(cancellationToken);
            await eventNotificationPublisher.PublishAsync(
                EventNotification.FromEvent(
                    calendarEvent,
                    EventNotificationType.Updated),
                cancellationToken);
        }

    }
}
