namespace AppointmentScheduler.Api.Responses;

public sealed record ListAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
