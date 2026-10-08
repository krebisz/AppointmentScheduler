namespace AppointmentScheduler.Application.Events.Create;

public sealed record CreatedAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
