using AppointmentScheduler.Application.Commands;
using AppointmentScheduler.Application.Exceptions;
using AppointmentScheduler.Application.Interfaces;
using AppointmentScheduler.Application.Notifications;

namespace AppointmentScheduler.Application.Handlers;

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
