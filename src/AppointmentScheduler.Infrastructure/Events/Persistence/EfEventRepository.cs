using AppointmentScheduler.Application.Events.Persistence;
using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Infrastructure.Events.Persistence;

public sealed class EfEventRepository(SchedulerDbContext dbContext) : IEventRepository
{
    public async Task AddAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken)
    {
        await dbContext.Events.AddAsync(calendarEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<CalendarEvent?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Events
            .Include(calendarEvent => calendarEvent.Attendees)
            .SingleOrDefaultAsync(
                calendarEvent => calendarEvent.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarEvent>> ListAsync(
        EventRepositoryQuery query,
        CancellationToken cancellationToken)
    {
        var events = dbContext.Events
            .AsNoTracking()
            .AsQueryable();

        if (!query.IncludeCancelled)
        {
            events = events.Where(calendarEvent => !calendarEvent.IsCancelled);
        }

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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new EventConcurrencyException(exception);
        }
    }
}
