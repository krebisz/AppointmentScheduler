namespace AppointmentScheduler.Api.Events.List;

public sealed record EventListItemResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsCancelled,
    long Version,
    IReadOnlyCollection<EventAttendeeResponse> Attendees);
