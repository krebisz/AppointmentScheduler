using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentScheduler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SchedulerDbContext>(
            options => options.UseSqlite(connectionString));
        services.AddScoped<IEventRepository, EfEventRepository>();

        return services;
    }
}
