using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Application.Events.List;
using AppointmentScheduler.Infrastructure;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<CreateEventHandler>();
builder.Services.AddScoped<ListEventsHandler>();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("SchedulerDatabase")
    ?? "Data Source=appointment-scheduler.db");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;
