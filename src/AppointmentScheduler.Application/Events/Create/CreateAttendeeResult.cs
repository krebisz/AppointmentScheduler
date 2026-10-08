namespace AppointmentScheduler.Application.Events.Create;

public sealed record CreateAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
