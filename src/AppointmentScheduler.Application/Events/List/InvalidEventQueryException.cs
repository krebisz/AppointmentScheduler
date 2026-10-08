namespace AppointmentScheduler.Application.Events.List;

public sealed class InvalidEventQueryException(string message) : Exception(message);
