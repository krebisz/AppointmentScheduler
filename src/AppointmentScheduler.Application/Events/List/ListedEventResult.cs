namespace AppointmentScheduler.Application.Events.List;

public sealed record ListedEventResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsCancelled,
    long Version,
    IReadOnlyCollection<ListedAttendeeResult> Attendees);
