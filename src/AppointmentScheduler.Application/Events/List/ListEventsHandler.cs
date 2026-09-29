namespace AppointmentScheduler.Application.Events.List;

public sealed class ListEventsHandler(IEventRepository eventRepository)
{
    public async Task<IReadOnlyList<ListedEventResult>> HandleAsync(
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
            new EventQuery(
                from,
                to,
                string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim()),
            cancellationToken);

        return events
            .Select(calendarEvent => new ListedEventResult(
                calendarEvent.Id,
                calendarEvent.Title,
                calendarEvent.Description,
                calendarEvent.StartTime,
                calendarEvent.EndTime,
                calendarEvent.Attendees
                    .Select(attendee => new ListedAttendeeResult(
                        attendee.Id,
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()))
            .ToArray();
    }
}

public sealed record ListEventsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search);

public sealed record ListedEventResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<ListedAttendeeResult> Attendees);

public sealed record ListedAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);

public sealed class InvalidEventQueryException(string message) : Exception(message);
