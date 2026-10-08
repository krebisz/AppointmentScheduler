namespace AppointmentScheduler.Application.Events.Get;

public sealed record GetEventResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsCancelled,
    long Version,
    IReadOnlyCollection<GetAttendeeResult> Attendees);
