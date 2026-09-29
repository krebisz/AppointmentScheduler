using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Application.Events;

public interface IEventRepository
{
    Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<CalendarEvent>> ListAsync(
        EventQuery query,
        CancellationToken cancellationToken);
}

public sealed record EventQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search);
