using AppointmentScheduler.Application.Commands;
using AppointmentScheduler.Application.Exceptions;
using AppointmentScheduler.Application.Interfaces;
using AppointmentScheduler.Application.Notifications;

namespace AppointmentScheduler.Application.Handlers;

public sealed class CancelEventHandler(
    IEventRepository eventRepository,
    IEventNotificationPublisher eventNotificationPublisher)
{
    public async Task HandleAsync(
        CancelEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await eventRepository.GetByIdAsync(
            command.EventId,
            cancellationToken)
            ?? throw new EventNotFoundException(command.EventId);

        if (!calendarEvent.Cancel())
        {
            return;
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        await eventNotificationPublisher.PublishAsync(
            EventNotification.FromEvent(calendarEvent, EventNotificationType.Cancelled),
            cancellationToken);
    }
}
