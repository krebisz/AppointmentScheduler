namespace AppointmentScheduler.Api.Events.Get;

public sealed record GetEventResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsCancelled,
    long Version,
    IReadOnlyCollection<GetAttendeeResponse> Attendees);
