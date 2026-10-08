namespace AppointmentScheduler.Application.Events.Update;

public sealed record UpdateEventResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    long Version,
    IReadOnlyCollection<UpdatedAttendeeResult> Attendees);
