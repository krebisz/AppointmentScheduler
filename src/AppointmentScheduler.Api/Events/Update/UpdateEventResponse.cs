namespace AppointmentScheduler.Api.Events.Update;

public sealed record UpdateEventResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    long Version,
    IReadOnlyCollection<UpdateAttendeeResponse> Attendees);
