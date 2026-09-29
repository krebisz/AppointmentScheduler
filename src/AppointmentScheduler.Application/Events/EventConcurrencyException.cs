namespace AppointmentScheduler.Application.Events;

public sealed class EventConcurrencyException : Exception
{
    public EventConcurrencyException(
        Guid eventId,
        long expectedVersion,
        long actualVersion)
        : base(
            $"Event '{eventId}' has version {actualVersion}; version {expectedVersion} was supplied.")
    {
    }

    public EventConcurrencyException(Exception innerException)
        : base(
            "The event changed while this operation was being saved. Reload it and retry.",
            innerException)
    {
    }
}
