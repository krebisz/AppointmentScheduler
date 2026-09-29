namespace AppointmentScheduler.Domain.Events;

public sealed class DomainValidationException(string message) : Exception(message);
