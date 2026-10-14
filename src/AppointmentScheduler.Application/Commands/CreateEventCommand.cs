using AppointmentScheduler.Application.Inputs;
namespace AppointmentScheduler.Application.Commands;

public sealed record CreateEventCommand(
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<CreateAttendeeInput> Attendees);
