using AppointmentScheduler.Application.Queries;
using AppointmentScheduler.Domain.Entities;

namespace AppointmentScheduler.Application.Interfaces;

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
