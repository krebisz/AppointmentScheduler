using AppointmentScheduler.Tests.Api;
using System.Net;
using System.Net.Http.Json;
using AppointmentScheduler.Api.Events.Create;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppointmentScheduler.Tests.Api.Events.Create;

public sealed class CreateEventEndpointTests(AppointmentSchedulerApiFactory factory)
    : IClassFixture<AppointmentSchedulerApiFactory>
{
    [Fact]
    public async Task Post_creates_event_and_returns_created_responseAsync()
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
    public async Task Post_rejects_invalid_time_range_without_persistingAsync()
    {
        using var client = factory.CreateClient();
        var valid = ValidRequest();
        var request = valid with { EndTime = valid.StartTime };

        var httpResponse = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
    }

    [Fact]
    public async Task Swagger_ui_is_available_in_developmentAsync()
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
