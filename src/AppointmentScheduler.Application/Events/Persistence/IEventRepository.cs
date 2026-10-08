using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Application.Events.Persistence;

public interface IEventRepository
{
    Task AddAndSaveAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);

    Task<CalendarEvent?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CalendarEvent>> ListAsync(
        EventRepositoryQuery query,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
