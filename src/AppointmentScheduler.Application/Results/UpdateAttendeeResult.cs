namespace AppointmentScheduler.Application.Results;

public sealed record UpdateAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
