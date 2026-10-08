namespace AppointmentScheduler.Application.Events.Notifications;

public interface IEventNotificationPublisher
{
    Task PublishAsync(
        EventNotification notification,
        CancellationToken cancellationToken);
}
