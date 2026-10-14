using AppointmentScheduler.Application.Notifications;
namespace AppointmentScheduler.Application.Interfaces;

public interface IEventNotificationPublisher
{
    Task PublishAsync(
        EventNotification notification,
        CancellationToken cancellationToken);
}
