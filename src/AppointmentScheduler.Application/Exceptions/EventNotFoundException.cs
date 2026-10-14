namespace AppointmentScheduler.Application.Exceptions;

public sealed class EventNotFoundException(Guid eventId)
    : Exception($"Event '{eventId}' was not found.");
