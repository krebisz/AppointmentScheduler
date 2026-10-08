namespace AppointmentScheduler.Application.Events.List;

public sealed record ListEventItemResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsCancelled,
    long Version,
    IReadOnlyCollection<ListAttendeeResult> Attendees);
