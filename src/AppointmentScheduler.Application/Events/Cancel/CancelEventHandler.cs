using AppointmentScheduler.Application.Events.Notifications;

namespace AppointmentScheduler.Application.Events.Cancel;

public sealed class CancelEventHandler(
    IEventRepository eventRepository,
    IEventNotificationPublisher eventNotificationPublisher)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await eventRepository.GetByIdAsync(
            id,
            cancellationToken)
            ?? throw new EventNotFoundException(id);

        if (!calendarEvent.Cancel())
        {
            return;
        }

        await eventRepository.SaveChangesAsync(cancellationToken);
        await eventNotificationPublisher.PublishAsync(
            EventNotification.From(calendarEvent, EventNotificationType.Cancelled),
            cancellationToken);
    }
}
