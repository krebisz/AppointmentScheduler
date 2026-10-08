using AppointmentScheduler.Application.Events.Persistence;
using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Infrastructure.Events.Persistence;

public sealed class EfEventRepository(SchedulerDbContext dbContext) : IEventRepository
{
    public async Task AddAndSaveAsync(
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
            dbContext.ChangeTracker.DetectChanges();
            var emailChanges = dbContext.ChangeTracker.Entries<Attendee>()
                .Where(entry => entry.State == EntityState.Modified
                    && entry.Property(attendee => attendee.EmailAddress).IsModified)
                .Select(entry => (
                    Property: entry.Property(attendee => attendee.EmailAddress),
                    FinalEmail: entry.Property(attendee => attendee.EmailAddress).CurrentValue))
                .ToArray();
            if (emailChanges.Length == 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            // Stage changed emails in one transaction so SQLite's immediate unique index permits swaps.
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var change in emailChanges)
                    change.Property.CurrentValue = $"{Guid.NewGuid():N}@scheduler.invalid";
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var change in emailChanges)
                    change.Property.CurrentValue = change.FinalEmail;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                foreach (var change in emailChanges)
                    change.Property.CurrentValue = change.FinalEmail;
            }
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new EventConcurrencyException(exception);
        }
    }
}
