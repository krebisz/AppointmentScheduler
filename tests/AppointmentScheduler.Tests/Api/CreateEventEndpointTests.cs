using System.Net;
using System.Net.Http.Json;
using AppointmentScheduler.Controllers;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AppointmentScheduler.Tests.Api;

public sealed class CreateEventEndpointTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Post_creates_event_and_returns_created_response()
    {
        using var client = factory.CreateClient();

        var httpResponse = await client.PostAsJsonAsync("/api/events", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);
        var response = await httpResponse.Content.ReadFromJsonAsync<CreateEventResponse>();
        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Single(response.Attendees);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var storedEvent = await dbContext.Events
            .Include(calendarEvent => calendarEvent.Attendees)
            .SingleAsync(calendarEvent => calendarEvent.Id == response.Id);

        Assert.Single(storedEvent.Attendees);
    }

    [Fact]
    public async Task Post_rejects_invalid_time_range_without_persisting()
    {
        using var client = factory.CreateClient();
        var valid = ValidRequest();
        var request = valid with { EndTime = valid.StartTime };

        var httpResponse = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
    }

    [Fact]
    public async Task Swagger_ui_is_available_in_development()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            "Swagger UI",
            await response.Content.ReadAsStringAsync());
    }

    private static CreateEventRequest ValidRequest()
    {
        return new CreateEventRequest(
            "Consultation",
            "Annual review",
            new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 1, 9, 30, 0, TimeSpan.Zero),
            [new CreateAttendeeRequest("Alex Patient", "alex@example.com", false)]);
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"appointment-scheduler-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SchedulerDbContext>>();
            services.AddDbContext<SchedulerDbContext>(
                options => options.UseSqlite(
                    $"Data Source={_databasePath};Pooling=False"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
        {
            SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
    }
}
