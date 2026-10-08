namespace AppointmentScheduler.Api.Events.Create;

public sealed record CreateAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
