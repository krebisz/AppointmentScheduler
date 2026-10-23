namespace AppointmentScheduler.Application.Exceptions;

public sealed class PractitionerConcurrencyException(Guid practitionerId)
    : Exception($"Practitioner '{practitionerId}' timeslot unavailable.");