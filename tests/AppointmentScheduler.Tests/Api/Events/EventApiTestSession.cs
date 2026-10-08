using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AppointmentScheduler.Application.Events.Notifications;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AppointmentScheduler.Tests.Api.Events;

internal sealed class EventApiTestSession : IDisposable
{
    private readonly AppointmentSchedulerApiFactory _factory = new();
    private readonly RecordingPublisher _publisher = new();
    private readonly WebApplicationFactory<Program> _host;

    public EventApiTestSession()
    {
        _host = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEventNotificationPublisher>();
            services.AddSingleton<IEventNotificationPublisher>(_publisher);
        }));
        Client = _host.CreateClient();
    }

    public HttpClient Client { get; }
    public IReadOnlyList<EventNotification> Notifications => _publisher.Notifications;
    public void ClearNotifications() => _publisher.Notifications.Clear();

    public static JsonObject ValidRequest() => JsonNode.Parse("""
        {"version":1,"title":"Consultation","description":"Annual review",
         "startTime":"2077-01-01T09:00:00Z","endTime":"2077-01-01T10:00:00Z",
         "attendees":[{"name":"Alex","emailAddress":"alex@example.com","isAttending":true}]}
        """)!.AsObject();

    public async Task<JsonObject> CreateEventAsync(JsonObject? body = null)
    {
        using var response = await Client.PostAsync("/api/events", JsonContent(body ?? ValidRequest()));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
    }

    public async Task<string> ReadStorageAsync()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var events = await scope.ServiceProvider.GetRequiredService<SchedulerDbContext>().Events
            .AsNoTracking().Include(calendarEvent => calendarEvent.Attendees)
            .OrderBy(calendarEvent => calendarEvent.Id).ToListAsync();
        return JsonSerializer.Serialize(events.Select(calendarEvent => new
        {
            calendarEvent.Id, calendarEvent.Title, calendarEvent.Description,
            calendarEvent.StartTime, calendarEvent.EndTime, calendarEvent.IsCancelled,
            calendarEvent.Version,
            Attendees = calendarEvent.Attendees.OrderBy(attendee => attendee.Id).Select(attendee => new
            {
                attendee.Id, attendee.Name, attendee.EmailAddress, attendee.IsAttending
            })
        }));
    }

    public async Task AssertRejectedWriteAsync(
        string method, string path, JsonNode body, HttpStatusCode expected = HttpStatusCode.BadRequest)
    {
        var before = await ReadStorageAsync();
        var notificationCount = Notifications.Count;
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent(body) };
        using var response = await Client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expected, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        if (expected == HttpStatusCode.BadRequest) Assert.NotEmpty(problem.Errors);
        Assert.Equal(before, await ReadStorageAsync());
        Assert.Equal(notificationCount, Notifications.Count);
    }

    public static StringContent JsonContent(JsonNode body) => new(body.ToJsonString(), Encoding.UTF8, "application/json");

    public void Dispose()
    {
        Client.Dispose();
        _host.Dispose();
        _factory.Dispose();
    }

    private sealed class RecordingPublisher : IEventNotificationPublisher
    {
        public List<EventNotification> Notifications { get; } = [];
        public Task PublishAsync(EventNotification notification, CancellationToken cancellationToken)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
