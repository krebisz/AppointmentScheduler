using AppointmentScheduler.Application.Events.Persistence;

namespace AppointmentScheduler.Application.Events.List;

public sealed class ListEventsHandler(IEventRepository eventRepository)
{
    public async Task<IReadOnlyList<ListEventItemResult>> HandleAsync(
        ListEventsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var from = query.From?.ToUniversalTime();
        var to = query.To?.ToUniversalTime();
        if (from.HasValue && to.HasValue && to < from)
        {
            throw new InvalidEventQueryException(
                "The 'to' filter must be later than or equal to the 'from' filter.");
        }

        var events = await eventRepository.ListAsync(
            new EventRepositoryQuery(
                from,
                to,
                string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
                query.IncludeCancelled),
            cancellationToken);

        return events
            .Select(calendarEvent => new ListEventItemResult(
                calendarEvent.Id,
                calendarEvent.Title,
                calendarEvent.Description,
                calendarEvent.StartTime,
                calendarEvent.EndTime,
                calendarEvent.IsCancelled,
                calendarEvent.Version,
                calendarEvent.Attendees
                    .Select(attendee => new ListAttendeeResult(
                        attendee.Id,
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()))
            .ToArray();
    }
}
