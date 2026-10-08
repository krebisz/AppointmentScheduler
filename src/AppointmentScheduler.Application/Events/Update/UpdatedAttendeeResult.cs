namespace AppointmentScheduler.Application.Events.Update;

public sealed record UpdatedAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
