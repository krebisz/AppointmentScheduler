namespace AppointmentScheduler.Application.Events.Create;

public sealed record CreateEventResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    long Version,
    IReadOnlyCollection<CreatedAttendeeResult> Attendees);
