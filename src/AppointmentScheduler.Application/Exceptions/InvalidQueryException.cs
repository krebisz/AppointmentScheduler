namespace AppointmentScheduler.Application.Exceptions;

public sealed class InvalidQueryException(string message) : Exception(message);
