using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Domain.Events;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyList<CalendarEvent>> ListAsync(
        EventQuery query,
        CancellationToken cancellationToken)
    {
        var events = dbContext.Events
            .AsNoTracking()
            .AsQueryable();

        if (query.From.HasValue)
        {
            events = events.Where(
                calendarEvent => calendarEvent.EndTime >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            events = events.Where(
                calendarEvent => calendarEvent.StartTime <= query.To.Value);
        }

        if (query.Search is not null)
        {
            var pattern = $"%{query.Search}%";
            events = events.Where(calendarEvent =>
                EF.Functions.Like(calendarEvent.Title, pattern)
                || EF.Functions.Like(calendarEvent.Description, pattern));
        }

        return await events
            .OrderBy(calendarEvent => calendarEvent.StartTime)
            .ThenBy(calendarEvent => calendarEvent.Id)
            .ToListAsync(cancellationToken);
    }
}
