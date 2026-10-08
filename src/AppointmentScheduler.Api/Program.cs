using AppointmentScheduler.Application.Events.Cancel;
using AppointmentScheduler.Application.Events.Create;
using AppointmentScheduler.Application.Events.Attendance;
using AppointmentScheduler.Application.Events.List;
using AppointmentScheduler.Application.Events.Update;
using AppointmentScheduler.Infrastructure;
using AppointmentScheduler.Infrastructure.Persistence;
using AppointmentScheduler.Api.ErrorHandling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<CreateEventHandler>();
builder.Services.AddScoped<ListEventsHandler>();
builder.Services.AddScoped<UpdateEventHandler>();
builder.Services.AddScoped<CancelEventHandler>();
builder.Services.AddScoped<SetAttendanceHandler>();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("SchedulerDatabase")
    ?? "Data Source=appointment-scheduler.db");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    await Results.Problem(statusCode: statusContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusContext.HttpContext);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Appointment Scheduler API v1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;
