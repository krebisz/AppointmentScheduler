using AppointmentScheduler.Application.Events.Persistence;

namespace AppointmentScheduler.Application.Events.Get;

public sealed class GetEventHandler(IEventRepository eventRepository)
{
    public async Task<GetEventResult> HandleAsync(GetEventQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var calendarEvent = await eventRepository.GetByIdAsync(query.EventId, cancellationToken)
            ?? throw new EventNotFoundException(query.EventId);
        return new GetEventResult(
            calendarEvent.Id, calendarEvent.Title, calendarEvent.Description,
            calendarEvent.StartTime, calendarEvent.EndTime, calendarEvent.IsCancelled,
            calendarEvent.Version,
            calendarEvent.Attendees.Select(attendee => new GetAttendeeResult(
                attendee.Id, attendee.Name, attendee.EmailAddress, attendee.IsAttending)).ToArray());
    }
}
