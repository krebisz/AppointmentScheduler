namespace AppointmentScheduler.Application.Events.List;

public sealed record ListAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
