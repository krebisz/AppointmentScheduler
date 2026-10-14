namespace AppointmentScheduler.Api.Responses;

public sealed record UpdateEventResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    long Version,
    IReadOnlyCollection<UpdateAttendeeResponse> Attendees);
