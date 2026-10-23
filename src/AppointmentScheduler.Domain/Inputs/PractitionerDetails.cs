namespace AppointmentScheduler.Domain.Inputs;

public sealed record PractitionerDetails(
    string FirstName,
    string Surname,
    bool IsActive);
