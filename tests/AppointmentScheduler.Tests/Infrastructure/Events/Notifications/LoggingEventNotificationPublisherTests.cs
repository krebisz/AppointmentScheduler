using AppointmentScheduler.Application.Events.Notifications;
using AppointmentScheduler.Infrastructure.Events.Notifications;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AppointmentScheduler.Tests.Infrastructure.Events.Notifications;

public sealed class LoggingEventNotificationPublisherTests
{
    [Fact]
    public async Task Publish_writes_an_observable_simulated_delivery_without_recipient_addresses()
    {
        var logger = new RecordingLogger<LoggingEventNotificationPublisher>();
        var publisher = new LoggingEventNotificationPublisher(logger);
        var eventId = Guid.NewGuid();

        await publisher.PublishAsync(
            new EventNotification(
                EventNotificationType.Created,
                eventId,
                "Consultation",
                new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 10, 1, 9, 30, 0, TimeSpan.Zero),
                ["alex@example.com", "sam@example.com"]),
            CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("Simulated event notification Created", entry.Message);
        Assert.Contains(eventId.ToString(), entry.Message);
        Assert.Contains("2 attendee recipient(s)", entry.Message);
        Assert.Contains("No external delivery was attempted", entry.Message);
        Assert.DoesNotContain("alex@example.com", entry.Message);
        Assert.DoesNotContain("sam@example.com", entry.Message);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);
}
