namespace AppointmentScheduler.Application.Results;

public sealed record UpdateEventResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    long Version,
    IReadOnlyCollection<UpdateAttendeeResult> Attendees);
