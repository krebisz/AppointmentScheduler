using System.Text.Json;
using AppointmentScheduler.Domain.Events;
using Xunit;

namespace AppointmentScheduler.Tests.Domain.Events;

public sealed class AttendeeReplacementTests
{
    [Fact]
    public void Replacement_retains_instances_adds_new_ids_and_removes_omitted_attendees()
    {
        var calendarEvent = CreateEvent();
        var retained = calendarEvent.Attendees.First();
        var removed = calendarEvent.Attendees.Last();
        calendarEvent.Update("Updated", "Updated description", calendarEvent.StartTime, calendarEvent.EndTime,
        [
            new AttendeeUpdateDetails("Renamed", "renamed@example.com", AttendeeId: retained.Id),
            new AttendeeUpdateDetails("Addition", removed.EmailAddress)
        ]);
        Assert.Same(retained, calendarEvent.Attendees.First());
        Assert.Equal("Renamed", retained.Name);
        Assert.True(retained.IsAttending);
        var addition = calendarEvent.Attendees.Last();
        Assert.NotEqual(removed.Id, addition.Id);
        Assert.False(addition.IsAttending);
        Assert.DoesNotContain(removed, calendarEvent.Attendees);
        Assert.Equal(2, calendarEvent.Version);
    }

    [Theory]
    [InlineData(null, null, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, true, false)]
    public void Attendance_preserves_existing_values_and_defaults_additions(
        bool? existingAttendance, bool? newAttendance, bool expectedExisting, bool expectedNew)
    {
        var calendarEvent = CreateEvent();
        var retained = calendarEvent.Attendees.First();
        calendarEvent.Update("Updated", "Test", calendarEvent.StartTime, calendarEvent.EndTime,
        [
            new AttendeeUpdateDetails("Alex", retained.EmailAddress, existingAttendance, retained.Id),
            new AttendeeUpdateDetails("Addition", "new@example.com", newAttendance)
        ]);
        Assert.Equal(expectedExisting, calendarEvent.Attendees.First().IsAttending);
        Assert.Equal(expectedNew, calendarEvent.Attendees.Last().IsAttending);
    }

    [Theory]
    [InlineData("unknownId")]
    [InlineData("duplicateId")]
    [InlineData("duplicateEmail")]
    [InlineData("invalidName")]
    [InlineData("invalidEmail")]
    [InlineData("invalidRange")]
    public void Invalid_replacement_leaves_entire_aggregate_and_existing_objects_unchanged(string invalid)
    {
        var calendarEvent = CreateEvent();
        var originals = calendarEvent.Attendees.ToArray();
        var before = JsonSerializer.Serialize(calendarEvent);
        var first = new AttendeeUpdateDetails("Changed", "changed@example.com", false, originals[0].Id);
        var second = invalid switch
        {
            "unknownId" => new AttendeeUpdateDetails("Other", "other@example.com", AttendeeId: Guid.NewGuid()),
            "duplicateId" => new AttendeeUpdateDetails("Other", "other@example.com", AttendeeId: originals[0].Id),
            "duplicateEmail" => new AttendeeUpdateDetails("Other", "CHANGED@example.com"),
            "invalidName" => new AttendeeUpdateDetails(" ", "other@example.com"),
            "invalidEmail" => new AttendeeUpdateDetails("Other", "invalid"),
            _ => new AttendeeUpdateDetails("Other", "other@example.com")
        };
        Assert.Throws<DomainValidationException>(() => calendarEvent.Update(
            "Changed title", "Changed description", calendarEvent.StartTime,
            invalid == "invalidRange" ? calendarEvent.StartTime : calendarEvent.EndTime,
            [first, second]));
        Assert.Equal(before, JsonSerializer.Serialize(calendarEvent));
        Assert.Same(originals[0], calendarEvent.Attendees.First());
        Assert.Same(originals[1], calendarEvent.Attendees.Last());
    }

    private static CalendarEvent CreateEvent() => CalendarEvent.Create(
        "Original", "Original description", DateTimeOffset.Parse("2077-01-01T09:00:00Z"),
        DateTimeOffset.Parse("2077-01-01T10:00:00Z"),
        [new AttendeeDetails("Alex", "alex@example.com", true), new AttendeeDetails("Sam", "sam@example.com", false)]);
}
