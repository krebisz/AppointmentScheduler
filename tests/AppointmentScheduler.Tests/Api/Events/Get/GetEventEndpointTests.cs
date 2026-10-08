using System.Net;
using System.Text.Json.Nodes;
using Xunit;

namespace AppointmentScheduler.Tests.Api.Events.Get;

public sealed class GetEventEndpointTests
{
    [Fact]
    public async Task Post_location_resolves_to_complete_event_without_read_side_effectsAsync()
    {
        using var session = new EventApiTestSession();
        using var creation = await session.Client.PostAsync("/api/events",
            EventApiTestSession.JsonContent(EventApiTestSession.ValidRequest()));
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var created = JsonNode.Parse(await creation.Content.ReadAsStringAsync())!;
        var location = creation.Headers.Location;
        Assert.NotNull(location);
        Assert.EndsWith($"/api/events/{created["id"]}", location.ToString());
        var before = await session.ReadStorageAsync();
        session.ClearNotifications();
        using var retrieval = await session.Client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, retrieval.StatusCode);
        var retrieved = JsonNode.Parse(await retrieval.Content.ReadAsStringAsync())!;
        foreach (var field in new[] { "id", "title", "description", "startTime", "endTime", "version", "attendees" })
            Assert.True(JsonNode.DeepEquals(created[field], retrieved[field]), field);
        Assert.False(retrieved["isCancelled"]!.GetValue<bool>());
        Assert.Equal(before, await session.ReadStorageAsync());
        Assert.Empty(session.Notifications);
    }

    [Fact]
    public async Task Cancelled_event_remains_retrievable_without_read_side_effectsAsync()
    {
        using var session = new EventApiTestSession();
        var created = await session.CreateEventAsync();
        var path = $"/api/events/{created["id"]}";
        using var cancellation = await session.Client.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.NoContent, cancellation.StatusCode);
        session.ClearNotifications();
        var before = await session.ReadStorageAsync();
        using var response = await session.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var retrieved = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.True(retrieved["isCancelled"]!.GetValue<bool>());
        Assert.Equal(2, retrieved["version"]!.GetValue<long>());
        Assert.True(JsonNode.DeepEquals(created["attendees"], retrieved["attendees"]));
        Assert.Equal(before, await session.ReadStorageAsync());
        Assert.Empty(session.Notifications);
    }

    [Fact]
    public async Task Missing_event_returns_404_without_read_side_effectsAsync()
    {
        using var session = new EventApiTestSession();
        await session.CreateEventAsync();
        session.ClearNotifications();
        var before = await session.ReadStorageAsync();
        using var response = await session.Client.GetAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal(404, problem["status"]!.GetValue<int>());
        Assert.Equal(before, await session.ReadStorageAsync());
        Assert.Empty(session.Notifications);
    }
}
