using AppointmentScheduler.Tests.Api;
using AppointmentScheduler.Application.Events.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using AppointmentScheduler.Application.Events;
using AppointmentScheduler.Application.Events.Notifications;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AppointmentScheduler.Tests.Api.ErrorHandling;

public sealed class ErrorHandlingTests
{
    private const string ValidBody = """
        {"version":1,"title":"Error test","description":"Test","startTime":"2077-01-01T09:00:00Z",
        "endTime":"2077-01-01T10:00:00Z","attendees":[{"name":"Alex","emailAddress":"alex@example.com","isAttending":false}]}
        """;

    [Theory]
    [InlineData("GET", "/api/events")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/events/11111111-1111-1111-1111-111111111111")]
    [InlineData("PATCH", "/api/events/11111111-1111-1111-1111-111111111111/attendees/22222222-2222-2222-2222-222222222222/attendance")]
    public async Task Unexpected_repository_errors_are_safe_problem_responses(string method, string path)
    {
        using var factory = new ApiFactory();
        using var faulty = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEventRepository>();
            services.AddScoped<IEventRepository, FailingRepository>();
        }));
        using var client = faulty.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT" or "PATCH")
            request.Content = new StringContent(method == "PATCH"
                ? """{"version":1,"isAttending":true}""" : ValidBody, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);
        await AssertProblem(response, HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private database failure", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("traceId", body);
    }

    [Theory]
    [InlineData("GET", "/api/events?from=invalid", "", HttpStatusCode.BadRequest)]
    [InlineData("GET", "/api/events?from=2077-02-01&to=2077-01-01", "", HttpStatusCode.BadRequest)]
    [InlineData("DELETE", "/api/events/11111111-1111-1111-1111-111111111111", "", HttpStatusCode.NotFound)]
    [InlineData("GET", "/unknown", "", HttpStatusCode.NotFound)]
    [InlineData("POST", "/api/events", "{broken", HttpStatusCode.BadRequest)]
    [InlineData("POST", "/api/events", "{}", HttpStatusCode.BadRequest)]
    [InlineData("POST", "/api/events", "null", HttpStatusCode.BadRequest)]
    [InlineData("POST", "/api/events", "", HttpStatusCode.BadRequest)]
    public async Task Expected_errors_return_problem_json(string method, string path, string body, HttpStatusCode expected)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST") request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        await AssertProblem(await client.SendAsync(request), expected);
    }

    [Theory]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/11111111-1111-1111-1111-111111111111")]
    public async Task Null_attendee_entries_are_validation_errors(string method, string path)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var body = ValidBody.Replace(
            """[{"name":"Alex","emailAddress":"alex@example.com","isAttending":false}]""", "[null]");
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        await AssertProblem(await client.SendAsync(request), HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("POST", "/api/events", "null")]
    [InlineData("POST", "/api/events", "[]")]
    [InlineData("PUT", "/api/events/11111111-1111-1111-1111-111111111111", "null")]
    [InlineData("PUT", "/api/events/11111111-1111-1111-1111-111111111111", "[]")]
    public async Task Null_or_empty_attendee_collections_are_validation_errors(string method, string path, string attendees)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var body = ValidBody.Replace(
            """[{"name":"Alex","emailAddress":"alex@example.com","isAttending":false}]""", attendees);
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        await AssertProblem(await client.SendAsync(request), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Notification_failure_propagates_as_500_after_data_has_been_saved()
    {
        using var factory = new ApiFactory();
        using var faulty = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEventNotificationPublisher>();
            services.AddScoped<IEventNotificationPublisher, FailingPublisher>();
        }));
        using var client = faulty.CreateClient();
        var response = await client.PostAsync("/api/events", new StringContent(ValidBody, Encoding.UTF8, "application/json"));
        await AssertProblem(response, HttpStatusCode.InternalServerError);
        using var scope = faulty.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<SchedulerDbContext>().Events.CountAsync());
    }

    [Theory]
    [InlineData("""{"version":1}""")]
    [InlineData("""{"version":1,"isAttending":null}""")]
    [InlineData("""{"version":0,"isAttending":true}""")]
    public async Task Attendance_requires_an_explicit_response_and_valid_version(string body)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PatchAsync(
            "/api/events/11111111-1111-1111-1111-111111111111/attendees/22222222-2222-2222-2222-222222222222/attendance",
            new StringContent(body, Encoding.UTF8, "application/json"));
        await AssertProblem(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("title", false)]
    [InlineData("description", false)]
    [InlineData("name", true)]
    [InlineData("emailAddress", true)]
    public async Task Null_required_fields_are_validation_errors(string property, bool nested)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var body = System.Text.Json.Nodes.JsonNode.Parse(ValidBody)!;
        var target = nested ? body["attendees"]![0]! : body;
        target[property] = null;
        await AssertProblem(await client.PostAsync("/api/events",
            new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Production_errors_are_also_safe_problem_responses()
    {
        using var factory = new ApiFactory();
        using var faulty = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEventRepository>();
                services.AddScoped<IEventRepository, FailingRepository>();
            });
        });
        using var client = faulty.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var response = await client.GetAsync("/api/events");
        await AssertProblem(response, HttpStatusCode.InternalServerError);
        Assert.DoesNotContain("private database failure", await response.Content.ReadAsStringAsync());
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expected, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }

    [Theory]
    [InlineData("PUT", false)]
    [InlineData("DELETE", false)]
    [InlineData("PATCH", false)]
    [InlineData("PUT", true)]
    [InlineData("DELETE", true)]
    [InlineData("PATCH", true)]
    public async Task Save_failures_propagate_without_publishing_notifications(string method, bool conflict)
    {
        var calendarEvent = CalendarEvent.Create("Save test", "Test",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            [new AttendeeDetails("Alex", "alex@example.com", false)]);
        var publisher = new CountingPublisher();
        using var factory = new ApiFactory();
        using var faulty = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEventRepository>();
            services.AddSingleton<IEventRepository>(new SaveFailingRepository(calendarEvent, conflict));
            services.RemoveAll<IEventNotificationPublisher>();
            services.AddSingleton<IEventNotificationPublisher>(publisher);
        }));
        using var client = faulty.CreateClient();
        var path = $"/api/events/{calendarEvent.Id}";
        if (method == "PATCH") path += $"/attendees/{calendarEvent.Attendees.Single().Id}/attendance";
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method != "DELETE")
            request.Content = new StringContent(method == "PATCH"
                ? """{"version":1,"isAttending":true}""" : ValidBody, Encoding.UTF8, "application/json");
        await AssertProblem(await client.SendAsync(request),
            conflict ? HttpStatusCode.Conflict : HttpStatusCode.InternalServerError);
        Assert.Equal(0, publisher.Calls);
    }

    private sealed class CountingPublisher : IEventNotificationPublisher
    {
        public int Calls { get; private set; }
        public Task PublishAsync(EventNotification notification, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class SaveFailingRepository(CalendarEvent calendarEvent, bool conflict) : IEventRepository
    {
        public Task AddAsync(CalendarEvent value, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<CalendarEvent?>(calendarEvent);
        public Task<IReadOnlyList<CalendarEvent>> ListAsync(EventRepositoryQuery query, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => throw (conflict
                ? new EventConcurrencyException(new DbUpdateConcurrencyException())
                : new InvalidOperationException("private save failure"));
    }

    private sealed class FailingPublisher : IEventNotificationPublisher
    {
        public Task PublishAsync(EventNotification notification, CancellationToken cancellationToken)
            => throw new InvalidOperationException("private notification failure");
    }

    private sealed class FailingRepository : IEventRepository
    {
        public Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken)
            => throw new InvalidOperationException("private database failure");
        public Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("private database failure");
        public Task<IReadOnlyList<CalendarEvent>> ListAsync(EventRepositoryQuery query, CancellationToken cancellationToken)
            => throw new InvalidOperationException("private database failure");
        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("private database failure");
    }
}
