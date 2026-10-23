namespace AppointmentScheduler.Application.Exceptions;

public sealed class PractitionerInactiveException(Guid practitionerId)
    : Exception($"Practitioner '{practitionerId}' has been deactivated.");