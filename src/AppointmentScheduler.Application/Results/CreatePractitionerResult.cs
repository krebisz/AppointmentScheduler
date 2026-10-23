
namespace  AppointmentScheduler.Application.Results;

public sealed record CreatePractitionerResult(Guid ID, string FirstName, string Surname, bool IsActive);

