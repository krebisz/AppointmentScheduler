namespace AppointmentScheduler.Domain.Inputs;

public sealed record PractitionerDetails(
    Guid ID,
    string FirstName,
    string Surname,
    bool IsActive);
