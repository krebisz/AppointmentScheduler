namespace AppointmentScheduler.Application.Events;

public sealed class EventNotFoundException(Guid eventId)
    : Exception($"Event '{eventId}' was not found.");
