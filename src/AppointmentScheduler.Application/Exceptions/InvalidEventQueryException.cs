namespace AppointmentScheduler.Application.Exceptions;

public sealed class InvalidEventQueryException(string message) : Exception(message);
