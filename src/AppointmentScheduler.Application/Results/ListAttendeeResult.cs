namespace AppointmentScheduler.Application.Results;

public sealed record ListAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
