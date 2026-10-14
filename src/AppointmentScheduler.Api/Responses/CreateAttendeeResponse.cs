namespace AppointmentScheduler.Api.Responses;

public sealed record CreateAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
