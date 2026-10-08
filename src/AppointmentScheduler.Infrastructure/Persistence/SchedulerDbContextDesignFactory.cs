using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppointmentScheduler.Infrastructure.Persistence;

public sealed class SchedulerDbContextDesignFactory : IDesignTimeDbContextFactory<SchedulerDbContext>
{
    public SchedulerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SchedulerDatabase")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__SchedulerDatabase explicitly before running migration commands.");
        return new SchedulerDbContext(new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(connectionString).Options);
    }
}
