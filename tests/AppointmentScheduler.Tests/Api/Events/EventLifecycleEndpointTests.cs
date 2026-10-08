using AppointmentScheduler.Tests.Api;
using System.Net;
using System.Net.Http.Json;
using AppointmentScheduler.Api.Events.Create;
using AppointmentScheduler.Api.Events.Update;
using AppointmentScheduler.Api.Events.List;
using AppointmentScheduler.Api.Events.Attendance;
using Xunit;

namespace AppointmentScheduler.Tests.Api.Events;

public sealed class EventLifecycleEndpointTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Put_replaces_event_details_and_attendees()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(
            client,
            "Original consultation",
            "Original description",
            "2060-01-01T09:00:00Z",
            "2060-01-01T09:30:00Z");

        var response = await client.PutAsJsonAsync(
            $"/api/events/{created.Id}",
            new
            {
                version = created.Version,
                title = "Updated consultation",
                description = "Updated description",
                startTime = "2060-01-01T10:00:00Z",
                endTime = "2060-01-01T11:00:00Z",
                attendees = new[]
                {
                    new
                    {
                        name = "New Patient",
                        emailAddress = "new@example.com",
                        isAttending = true
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UpdateEventResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated consultation", updated.Title);
        var attendee = Assert.Single(updated.Attendees);
        Assert.Equal("new@example.com", attendee.EmailAddress);
        Assert.True(attendee.IsAttending);

        var listed = await client.GetFromJsonAsync<EventListItemResponse[]>(
            "/api/events?search=updated");
        var listedEvent = Assert.Single(listed!);
        Assert.Equal(created.Id, listedEvent.Id);
        Assert.Single(listedEvent.Attendees);
    }

    [Fact]
    public async Task Delete_soft_cancels_and_is_idempotent()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(
            client,
            "Cancellation candidate",
            "Will be cancelled",
            "2070-01-01T09:00:00Z",
            "2070-01-01T09:30:00Z");

        var firstDelete = await client.DeleteAsync($"/api/events/{created.Id}");
        var secondDelete = await client.DeleteAsync($"/api/events/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondDelete.StatusCode);

        var activeEvents = await client.GetFromJsonAsync<EventListItemResponse[]>(
            "/api/events?search=Cancellation%20candidate");
        Assert.Empty(activeEvents!);

        var allEvents = await client.GetFromJsonAsync<EventListItemResponse[]>(
            "/api/events?search=Cancellation%20candidate&includeCancelled=true");
        var cancelledEvent = Assert.Single(allEvents!);
        Assert.True(cancelledEvent.IsCancelled);
    }

    [Fact]
    public async Task Put_returns_conflict_when_the_supplied_version_is_stale()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(
            client,
            "Concurrency candidate",
            "Original value",
            "2075-01-01T09:00:00Z",
            "2075-01-01T09:30:00Z");
        var request = new
        {
            version = created.Version,
            title = "First accepted update",
            description = "Updated once",
            startTime = "2075-01-01T10:00:00Z",
            endTime = "2075-01-01T10:30:00Z",
            attendees = new[]
            {
                new
                {
                    name = "Alex Patient",
                    emailAddress = "alex@example.com",
                    isAttending = false
                }
            }
        };

        var firstResponse = await client.PutAsJsonAsync(
            $"/api/events/{created.Id}",
            request);
        var staleResponse = await client.PutAsJsonAsync(
            $"/api/events/{created.Id}",
            request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
    }

    [Fact]
    public async Task Patch_attendance_updates_the_attendee_and_event_version()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(
            client,
            "Attendance candidate",
            "Awaiting response",
            "2076-01-01T09:00:00Z",
            "2076-01-01T09:30:00Z");
        var attendee = Assert.Single(created.Attendees);

        var response = await client.PatchAsJsonAsync(
            $"/api/events/{created.Id}/attendees/{attendee.Id}/attendance",
            new
            {
                isAttending = true,
                version = created.Version
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var events = await client.GetFromJsonAsync<EventListItemResponse[]>(
            "/api/events?search=Attendance%20candidate");
        var listedEvent = Assert.Single(events!);
        Assert.True(Assert.Single(listedEvent.Attendees).IsAttending);
        Assert.Equal(created.Version + 1, listedEvent.Version);
    }

    [Fact]
    public async Task Put_and_delete_return_not_found_for_an_unknown_event()
    {
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/events/{id}",
            new
            {
                version = 1,
                title = "Missing",
                description = "Missing event",
                startTime = "2080-01-01T09:00:00Z",
                endTime = "2080-01-01T09:30:00Z",
                attendees = new[]
                {
                    new
                    {
                        name = "Alex Patient",
                        emailAddress = "alex@example.com",
                        isAttending = false
                    }
                }
            });
        var deleteResponse = await client.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    private static async Task<CreateEventResponse> CreateEventAsync(
        HttpClient client,
        string title,
        string description,
        string startTime,
        string endTime)
    {
        var response = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                title,
                description,
                startTime,
                endTime,
                attendees = new[]
                {
                    new
                    {
                        name = "Alex Patient",
                        emailAddress = "alex@example.com",
                        isAttending = false
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreateEventResponse>())!;
    }
}
