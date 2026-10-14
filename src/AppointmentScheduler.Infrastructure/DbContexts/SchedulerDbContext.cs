using AppointmentScheduler.Domain.Entities;
using AppointmentScheduler.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Infrastructure.DbContexts;

public sealed class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options)
    : DbContext(options)
{
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CalendarEventConfiguration());
    }
}
