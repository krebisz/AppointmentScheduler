namespace AppointmentScheduler.Application.Exceptions;

public sealed class PractitionerNotFoundException(Guid practitionerId)
    : Exception($"Practitioner '{practitionerId}' was not found.");