namespace AppointmentScheduler.Api.Events.List;

public sealed record ListAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
