using Xunit;

namespace AppointmentScheduler.Tests.Api.Events.Validation;

public sealed class InvalidEventWriteTests
{
    [Theory]
    [InlineData("POST", "title")]
    [InlineData("PUT", "title")]
    [InlineData("POST", "description")]
    [InlineData("PUT", "description")]
    [InlineData("POST", "name")]
    [InlineData("PUT", "name")]
    [InlineData("POST", "emailAddress")]
    [InlineData("PUT", "emailAddress")]
    [InlineData("POST", "emptyAttendees")]
    [InlineData("PUT", "emptyAttendees")]
    [InlineData("POST", "nullAttendee")]
    [InlineData("PUT", "nullAttendee")]
    [InlineData("POST", "duplicateEmail")]
    [InlineData("PUT", "duplicateEmail")]
    [InlineData("POST", "range")]
    [InlineData("PUT", "range")]
    public async Task Invalid_writes_leave_storage_and_notifications_unchangedAsync(string method, string invalid)
    {
        using var session = new EventApiTestSession();
        var created = await session.CreateEventAsync();
        session.ClearNotifications();
        var body = EventApiTestSession.ValidRequest();
        switch (invalid)
        {
            case "title": case "description": body[invalid] = null; break;
            case "name": case "emailAddress": body["attendees"]![0]![invalid] = null; break;
            case "emptyAttendees": body["attendees"] = new System.Text.Json.Nodes.JsonArray(); break;
            case "nullAttendee": body["attendees"]![0] = null; break;
            case "duplicateEmail":
                var duplicate = body["attendees"]![0]!.DeepClone();
                duplicate["emailAddress"] = "ALEX@example.com";
                body["attendees"]!.AsArray().Add(duplicate);
                break;
            case "range": body["endTime"] = body["startTime"]!.DeepClone(); break;
        }
        var path = method == "POST" ? "/api/events" : $"/api/events/{created["id"]}";
        await session.AssertRejectedWriteAsync(method, path, body);
        Assert.Empty(session.Notifications);
    }
}
