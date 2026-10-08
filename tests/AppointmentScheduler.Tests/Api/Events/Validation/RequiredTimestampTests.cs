using System.Net;
using System.Text.Json.Nodes;
using Xunit;

namespace AppointmentScheduler.Tests.Api.Events.Validation;

public sealed class RequiredTimestampTests
{
    public static IEnumerable<object[]> InvalidTimestamps()
    {
        foreach (var method in new[] { "POST", "PUT" })
        foreach (var field in new[] { "startTime", "endTime" })
        foreach (var value in new[] { "omitted", "null", "malformed", "number" })
            yield return [method, field, value];
    }

    [Theory]
    [MemberData(nameof(InvalidTimestamps))]
    public async Task Missing_null_or_malformed_timestamps_reject_without_side_effectsAsync(
        string method, string field, string value)
    {
        using var session = new EventApiTestSession();
        var created = await session.CreateEventAsync();
        session.ClearNotifications();
        var body = EventApiTestSession.ValidRequest();
        switch (value)
        {
            case "omitted": body.Remove(field); break;
            case "null": body[field] = null; break;
            case "malformed": body[field] = "not-a-timestamp"; break;
            case "number": body[field] = 123; break;
        }
        await session.AssertRejectedWriteAsync(method,
            method == "POST" ? "/api/events" : $"/api/events/{created["id"]}", body);
        Assert.Empty(session.Notifications);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Inverted_timestamps_reject_without_side_effectsAsync(string method)
    {
        using var session = new EventApiTestSession();
        var created = await session.CreateEventAsync();
        session.ClearNotifications();
        var body = EventApiTestSession.ValidRequest();
        body["endTime"] = "2077-01-01T08:00:00Z";
        await session.AssertRejectedWriteAsync(method,
            method == "POST" ? "/api/events" : $"/api/events/{created["id"]}", body);
    }

    [Fact]
    public async Task Explicit_valid_timestamps_create_and_update_as_nonnullable_utc_valuesAsync()
    {
        using var session = new EventApiTestSession();
        var body = EventApiTestSession.ValidRequest();
        body["startTime"] = "2077-01-01T11:00:00+02:00";
        body["endTime"] = "2077-01-01T12:00:00+02:00";
        var created = await session.CreateEventAsync(body);
        Assert.Equal(DateTimeOffset.Parse("2077-01-01T09:00:00Z"), created["startTime"]!.GetValue<DateTimeOffset>());
        using var response = await session.Client.PutAsync($"/api/events/{created["id"]}", EventApiTestSession.JsonContent(body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal(DateTimeOffset.Parse("2077-01-01T10:00:00Z"), updated["endTime"]!.GetValue<DateTimeOffset>());
        Assert.Equal(2, session.Notifications.Count);
    }
}
