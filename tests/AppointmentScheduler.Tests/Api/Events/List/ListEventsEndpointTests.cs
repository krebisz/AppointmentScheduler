using AppointmentScheduler.Tests.Api;
using System.Net;
using System.Net.Http.Json;
using AppointmentScheduler.Api.Events.Create;
using AppointmentScheduler.Api.Events.Update;
using AppointmentScheduler.Api.Events.List;
using AppointmentScheduler.Api.Events.Attendance;
using Xunit;

namespace AppointmentScheduler.Tests.Api.Events.List;

public sealed class ListEventsEndpointTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Get_filters_events_that_overlap_the_requested_utc_range()
    {
        using var client = factory.CreateClient();
        await CreateEventAsync(
            client,
            "Before range",
            "Does not overlap",
            "2030-01-01T08:00:00Z",
            "2030-01-01T08:30:00Z");
        await CreateEventAsync(
            client,
            "Overlapping consultation",
            "Overlaps the requested range",
            "2030-01-01T09:00:00Z",
            "2030-01-01T10:00:00Z");
        await CreateEventAsync(
            client,
            "After range",
            "Does not overlap",
            "2030-01-01T11:00:00Z",
            "2030-01-01T11:30:00Z");

        var events = await client.GetFromJsonAsync<EventListItemResponse[]>(
            "/api/events?from=2030-01-01T09:30:00Z&to=2030-01-01T10:30:00Z");

        var calendarEvent = Assert.Single(events!);
        Assert.Equal("Overlapping consultation", calendarEvent.Title);
        Assert.Single(calendarEvent.Attendees);
    }

    [Fact]
    public async Task Get_searches_title_and_description_case_insensitively()
    {
        using var client = factory.CreateClient();
        await CreateEventAsync(
            client,
            "Cardiology follow-up",
            "Review test results",
            "2040-01-01T09:00:00Z",
            "2040-01-01T09:30:00Z");
        await CreateEventAsync(
            client,
            "Routine visit",
            "Discuss CARDIOLOGY referral",
            "2040-01-01T10:00:00Z",
            "2040-01-01T10:30:00Z");
        await CreateEventAsync(
            client,
            "Vaccination",
            "Annual booster",
            "2040-01-01T11:00:00Z",
            "2040-01-01T11:30:00Z");

        var events = await client.GetFromJsonAsync<EventListItemResponse[]>(
            "/api/events?search=cardiology");

        Assert.NotNull(events);
        Assert.Equal(
            ["Cardiology follow-up", "Routine visit"],
            events.Select(calendarEvent => calendarEvent.Title).ToArray());
    }

    [Fact]
    public async Task Get_rejects_an_inverted_date_range()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/events?from=2050-01-02T00:00:00Z&to=2050-01-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task CreateEventAsync(
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
    }
}
