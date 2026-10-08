namespace AppointmentScheduler.Api.Events.Create;

public sealed record CreateEventResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    long Version,
    IReadOnlyCollection<CreateAttendeeResponse> Attendees);
