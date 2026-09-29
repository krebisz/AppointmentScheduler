using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Application.Events;

public interface IEventRepository
{
    Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);
}
