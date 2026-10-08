namespace AppointmentScheduler.Api.Events.Update;

public sealed record UpdateAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
