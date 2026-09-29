namespace AppointmentScheduler.Application.Events;

public sealed class EventNotFoundException(Guid id)
    : Exception($"Event '{id}' was not found.");
