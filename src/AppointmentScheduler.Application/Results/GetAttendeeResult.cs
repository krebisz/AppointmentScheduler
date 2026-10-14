namespace AppointmentScheduler.Application.Results;

public sealed record GetAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
