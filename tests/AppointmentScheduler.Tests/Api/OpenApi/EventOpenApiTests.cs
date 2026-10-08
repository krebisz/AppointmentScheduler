using System.Text.Json.Nodes;
using AppointmentScheduler.Tests.Api.Events;
using Xunit;

namespace AppointmentScheduler.Tests.Api.OpenApi;

public sealed class EventOpenApiTests
{
    [Fact]
    public async Task Document_describes_required_timestamps_retrieval_location_and_attendee_update_semanticsAsync()
    {
        using var session = new EventApiTestSession();
        using var response = await session.Client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var schemas = document["components"]!["schemas"]!;
        foreach (var name in new[] { "CreateEventRequest", "UpdateEventRequest" })
        {
            var schema = schemas[name]!;
            var required = schema["required"]!.AsArray().Select(value => value!.GetValue<string>()).ToArray();
            Assert.Contains("startTime", required);
            Assert.Contains("endTime", required);
            Assert.Equal("string", schema["properties"]!["startTime"]!["type"]!.GetValue<string>());
            Assert.Equal("date-time", schema["properties"]!["endTime"]!["format"]!.GetValue<string>());
        }
        Assert.Contains("removed", schemas["UpdateEventRequest"]!["properties"]!["attendees"]!["description"]!.GetValue<string>());
        var attendee = schemas["UpdateAttendeeRequest"]!;
        var attendeeRequired = attendee["required"]!.AsArray().Select(value => value!.GetValue<string>()).ToArray();
        Assert.DoesNotContain("id", attendeeRequired);
        Assert.DoesNotContain("isAttending", attendeeRequired);
        Assert.Contains("preserve", attendee["properties"]!["isAttending"]!["description"]!.GetValue<string>());
        Assert.Contains("email does not identify", attendee["properties"]!["id"]!["description"]!.GetValue<string>());
        var paths = document["paths"]!;
        Assert.NotNull(paths["/api/events/{id}"]!["get"]!["responses"]!["200"]);
        Assert.NotNull(paths["/api/events/{id}"]!["get"]!["responses"]!["404"]);
        Assert.NotNull(paths["/api/events"]!["post"]!["responses"]!["201"]!["headers"]!["Location"]);
    }
}
