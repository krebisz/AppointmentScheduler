namespace AppointmentScheduler.Api.Events.Update;

public sealed record UpdatedAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
