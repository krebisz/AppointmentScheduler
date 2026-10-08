namespace AppointmentScheduler.Application.Events.Get;

public sealed record GetAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
