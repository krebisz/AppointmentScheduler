
namespace AppointmentScheduler.Application.Commands;

public sealed record CreatePractitionerCommand(string FirstName,
    string Surname,
    bool IsActive);