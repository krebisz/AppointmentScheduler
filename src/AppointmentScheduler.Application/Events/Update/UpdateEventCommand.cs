namespace AppointmentScheduler.Application.Events.Update;

public sealed record UpdateEventCommand(
    Guid Id,
    long Version,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<UpdateAttendeeCommand> Attendees);
