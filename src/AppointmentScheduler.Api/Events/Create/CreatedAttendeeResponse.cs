namespace AppointmentScheduler.Api.Events.Create;

public sealed record CreatedAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
