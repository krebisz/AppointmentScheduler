namespace AppointmentScheduler.Application.Events.List;

public sealed record ListedAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
