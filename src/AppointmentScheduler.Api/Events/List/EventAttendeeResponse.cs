namespace AppointmentScheduler.Api.Events.List;

public sealed record EventAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
