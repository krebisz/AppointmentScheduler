namespace AppointmentScheduler.Api.Events.Get;

public sealed record GetAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
