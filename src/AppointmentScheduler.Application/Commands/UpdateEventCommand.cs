using AppointmentScheduler.Application.Inputs;
namespace AppointmentScheduler.Application.Commands;

public sealed record UpdateEventCommand(
    Guid EventId,
    long ExpectedVersion,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<UpdateAttendeeInput> Attendees);
