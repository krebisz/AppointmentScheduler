namespace AppointmentScheduler.Application.Events.Update;

public sealed record UpdateAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
