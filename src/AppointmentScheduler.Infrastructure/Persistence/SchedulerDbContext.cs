using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Infrastructure.Events.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Infrastructure.Persistence;

public sealed class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options)
    : DbContext(options)
{
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CalendarEventConfiguration());
    }
}
