namespace AppointmentScheduler.Application.Results;

public sealed record CreateAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
