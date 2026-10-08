namespace AppointmentScheduler.Application.Events.Create;

public sealed record CreateEventCommand(
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<CreateAttendeeCommand> Attendees);
