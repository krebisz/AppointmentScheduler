using System.Net;
using System.Text.Json.Nodes;
using AppointmentScheduler.Application.Events.Notifications;
using Xunit;

namespace AppointmentScheduler.Tests.Api.Events.Update;

public sealed class UpdateAttendeeIdentityTests
{
    [Fact]
    public async Task Existing_attendees_can_exchange_unique_email_addresses_without_changing_identityAsync()
    {
        using var session = new EventApiTestSession();
        var createBody = EventApiTestSession.ValidRequest();
        createBody["attendees"]!.AsArray().Add(new JsonObject
        {
            ["name"] = "Sam", ["emailAddress"] = "sam@example.com", ["isAttending"] = false
        });
        var created = await session.CreateEventAsync(createBody);
        var originals = created["attendees"]!.AsArray();
        var body = EventApiTestSession.ValidRequest();
        body["attendees"] = new JsonArray(
            new JsonObject { ["id"] = originals[0]!["id"]!.DeepClone(), ["name"] = "Alex", ["emailAddress"] = "sam@example.com" },
            new JsonObject { ["id"] = originals[1]!["id"]!.DeepClone(), ["name"] = "Sam", ["emailAddress"] = "alex@example.com" });
        session.ClearNotifications();
        using var response = await session.Client.PutAsync($"/api/events/{created["id"]}", EventApiTestSession.JsonContent(body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var retrieval = await session.Client.GetAsync($"/api/events/{created["id"]}");
        var persisted = JsonNode.Parse(await retrieval.Content.ReadAsStringAsync())!["attendees"]!.AsArray();
        foreach (var replacement in body["attendees"]!.AsArray())
        {
            var attendee = Assert.Single(persisted, item => JsonNode.DeepEquals(item!["id"], replacement!["id"]))!;
            Assert.True(JsonNode.DeepEquals(replacement!["emailAddress"], attendee["emailAddress"]));
        }
        Assert.Single(session.Notifications);
    }

    [Fact]
    public async Task Replacement_reconciles_retained_added_and_removed_sqlite_childrenAsync()
    {
        using var session = new EventApiTestSession();
        var createBody = EventApiTestSession.ValidRequest();
        createBody["attendees"]!.AsArray().Add(new JsonObject
        {
            ["name"] = "Sam", ["emailAddress"] = "sam@example.com", ["isAttending"] = false
        });
        var created = await session.CreateEventAsync(createBody);
        var original = created["attendees"]!.AsArray();
        var retainedId = original[0]!["id"]!.GetValue<Guid>();
        var removedId = original[1]!["id"]!.GetValue<Guid>();
        var body = EventApiTestSession.ValidRequest();
        body["attendees"] = new JsonArray(
            new JsonObject { ["id"] = retainedId, ["name"] = "Renamed", ["emailAddress"] = "renamed@example.com" },
            new JsonObject { ["name"] = "Addition", ["emailAddress"] = "sam@example.com" });
        session.ClearNotifications();
        using var response = await session.Client.PutAsync($"/api/events/{created["id"]}", EventApiTestSession.JsonContent(body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal(2, updated["version"]!.GetValue<long>());
        using var retrieval = await session.Client.GetAsync($"/api/events/{created["id"]}");
        Assert.Equal(HttpStatusCode.OK, retrieval.StatusCode);
        var persisted = JsonNode.Parse(await retrieval.Content.ReadAsStringAsync())!["attendees"]!.AsArray();
        Assert.Equal(2, persisted.Count);
        var retained = Assert.Single(persisted, attendee => attendee!["id"]!.GetValue<Guid>() == retainedId)!;
        Assert.Equal("Renamed", retained["name"]!.GetValue<string>());
        Assert.True(retained["isAttending"]!.GetValue<bool>());
        var added = Assert.Single(persisted, attendee => attendee!["emailAddress"]!.GetValue<string>() == "sam@example.com")!;
        Assert.NotEqual(removedId, added["id"]!.GetValue<Guid>());
        Assert.False(added["isAttending"]!.GetValue<bool>());
        Assert.DoesNotContain(persisted, attendee => attendee!["id"]!.GetValue<Guid>() == removedId);
        Assert.Equal(EventNotificationType.Updated, Assert.Single(session.Notifications).Type);
        Assert.Equal(2, session.Notifications.Single().RecipientEmailAddresses.Count);
        // A removed child's former ID cannot be reintroduced as an existing attendee.
        body["version"] = 2;
        body["attendees"]![0]!["id"] = removedId;
        await session.AssertRejectedWriteAsync("PUT", $"/api/events/{created["id"]}", body);
    }

    [Theory]
    [InlineData(null, null, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, true, false)]
    public async Task Attendance_replacement_semantics_persist_through_sqliteAsync(
        bool? existingAttendance, bool? newAttendance, bool expectedExisting, bool expectedNew)
    {
        using var session = new EventApiTestSession();
        var created = await session.CreateEventAsync();
        var body = EventApiTestSession.ValidRequest();
        var existing = body["attendees"]![0]!;
        existing["id"] = created["attendees"]![0]!["id"]!.DeepClone();
        existing.AsObject().Remove("isAttending");
        if (existingAttendance.HasValue) existing["isAttending"] = existingAttendance.Value;
        var addition = new JsonObject { ["name"] = "Addition", ["emailAddress"] = "new@example.com" };
        if (newAttendance.HasValue) addition["isAttending"] = newAttendance.Value;
        body["attendees"]!.AsArray().Add(addition);
        using var response = await session.Client.PutAsync($"/api/events/{created["id"]}", EventApiTestSession.JsonContent(body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var retrieval = await session.Client.GetAsync($"/api/events/{created["id"]}");
        var attendees = JsonNode.Parse(await retrieval.Content.ReadAsStringAsync())!["attendees"]!.AsArray();
        Assert.Equal(expectedExisting, attendees.Single(item => item!["emailAddress"]!.GetValue<string>() == "alex@example.com")!["isAttending"]!.GetValue<bool>());
        Assert.Equal(expectedNew, attendees.Single(item => item!["emailAddress"]!.GetValue<string>() == "new@example.com")!["isAttending"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("unknownId")]
    [InlineData("foreignId")]
    [InlineData("duplicateId")]
    [InlineData("duplicateEmail")]
    [InlineData("malformedId")]
    [InlineData("staleVersion")]
    [InlineData("cancelled")]
    public async Task Rejected_replacements_preserve_storage_and_publish_nothingAsync(string invalid)
    {
        using var session = new EventApiTestSession();
        var created = await session.CreateEventAsync();
        var body = EventApiTestSession.ValidRequest();
        var first = body["attendees"]![0]!;
        first["id"] = created["attendees"]![0]!["id"]!.DeepClone();
        first["name"] = "Would change";
        first["isAttending"] = false;
        switch (invalid)
        {
            case "unknownId": first["id"] = Guid.NewGuid(); break;
            case "foreignId":
                var other = await session.CreateEventAsync();
                first["id"] = other["attendees"]![0]!["id"]!.DeepClone();
                break;
            case "duplicateId":
                var duplicate = first.DeepClone();
                duplicate["emailAddress"] = "other@example.com";
                body["attendees"]!.AsArray().Add(duplicate);
                break;
            case "duplicateEmail":
                body["attendees"]!.AsArray().Add(new JsonObject { ["name"] = "Other", ["emailAddress"] = "ALEX@example.com" });
                break;
            case "malformedId": first["id"] = "not-a-guid"; break;
            case "staleVersion": body["version"] = 99; break;
            case "cancelled":
                using (var cancellation = await session.Client.DeleteAsync($"/api/events/{created["id"]}"))
                    Assert.Equal(HttpStatusCode.NoContent, cancellation.StatusCode);
                body["version"] = 2;
                break;
        }
        session.ClearNotifications();
        await session.AssertRejectedWriteAsync("PUT", $"/api/events/{created["id"]}", body,
            invalid == "staleVersion" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest);
        Assert.Empty(session.Notifications);
    }
}
