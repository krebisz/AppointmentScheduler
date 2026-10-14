namespace AppointmentScheduler.Api.Responses;

public sealed record GetAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
