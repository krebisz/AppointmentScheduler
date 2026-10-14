namespace AppointmentScheduler.Api.Responses;

public sealed record UpdateAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
