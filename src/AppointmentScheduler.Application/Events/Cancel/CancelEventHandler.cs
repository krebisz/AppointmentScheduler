using AppointmentScheduler.Application.Events.Persistence;
using AppointmentScheduler.Application.Events.Notifications;

namespace AppointmentScheduler.Application.Events.Cancel;

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
