using AppointmentScheduler.Application.Events.Notifications;
using Microsoft.Extensions.Logging;

namespace AppointmentScheduler.Infrastructure.Events.Notifications;

public sealed class LoggingEventNotificationPublisher(
    ILogger<LoggingEventNotificationPublisher> logger)
    : IEventNotificationPublisher
{
    public Task PublishAsync(
        EventNotification notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Simulated event notification {NotificationType} for event {EventId} to {RecipientCount} attendee recipient(s). No external delivery was attempted.",
            notification.Type,
            notification.EventId,
            notification.RecipientEmailAddresses.Count);

        return Task.CompletedTask;
    }
}
