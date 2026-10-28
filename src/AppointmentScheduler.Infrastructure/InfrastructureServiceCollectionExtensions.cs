using AppointmentScheduler.Application.Interfaces;
using AppointmentScheduler.Infrastructure.DbContexts;
using AppointmentScheduler.Infrastructure.NotificationPublishers;
using AppointmentScheduler.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentScheduler.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SchedulerDbContext>(
            options => options.UseSqlite(connectionString));
        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IPractitionerRepository, EfPractitionerRepository>();
        services.AddScoped<IEventNotificationPublisher, LoggingEventNotificationPublisher>();

        return services;
    }
}
