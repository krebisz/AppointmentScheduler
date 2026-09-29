using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Infrastructure.Persistence;

public sealed class EfEventRepository(SchedulerDbContext dbContext) : IEventRepository
{
    public async Task AddAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.Events.AddAsync(calendarEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
