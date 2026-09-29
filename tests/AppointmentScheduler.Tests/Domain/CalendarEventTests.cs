using AppointmentScheduler.Domain.Events;
using Xunit;

namespace AppointmentScheduler.Tests.Domain;

public sealed class CalendarEventTests
{
    [Fact]
    public void Create_rejects_end_time_that_is_not_after_start_time()
    {
        var start = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

        var exception = Assert.Throws<DomainValidationException>(() =>
            CalendarEvent.Create(
                "Consultation",
                "Annual review",
                start,
                start,
                [new AttendeeDetails("Alex Patient", "alex@example.com", false)]));

        Assert.Equal("End time must be later than start time.", exception.Message);
    }

    [Fact]
    public void Create_rejects_duplicate_attendee_email_addresses()
    {
        var start = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

        var exception = Assert.Throws<DomainValidationException>(() =>
            CalendarEvent.Create(
                "Consultation",
                "Annual review",
                start,
                start.AddMinutes(30),
                [
                    new AttendeeDetails("Alex Patient", "alex@example.com", false),
                    new AttendeeDetails("Alex Duplicate", "ALEX@example.com", true)
                ]));

        Assert.Equal("Attendee email addresses must be unique.", exception.Message);
    }

    [Fact]
    public void Update_replaces_details_and_attendees()
    {
        var start = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
        var calendarEvent = CalendarEvent.Create(
            "Original",
            "Original description",
            start,
            start.AddMinutes(30),
            [new AttendeeDetails("Original Patient", "original@example.com", false)]);

        calendarEvent.Update(
            "Updated",
            "Updated description",
            start.AddHours(1),
            start.AddHours(2),
            [new AttendeeDetails("New Patient", "new@example.com", true)]);

        Assert.Equal("Updated", calendarEvent.Title);
        Assert.Equal(start.AddHours(1), calendarEvent.StartTime);
        var attendee = Assert.Single(calendarEvent.Attendees);
        Assert.Equal("new@example.com", attendee.EmailAddress);
        Assert.True(attendee.IsAttending);
    }

    [Fact]
    public void Update_rejects_a_cancelled_event()
    {
        var start = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
        var calendarEvent = CalendarEvent.Create(
            "Consultation",
            "Annual review",
            start,
            start.AddMinutes(30),
            [new AttendeeDetails("Alex Patient", "alex@example.com", false)]);
        calendarEvent.Cancel();

        var exception = Assert.Throws<DomainValidationException>(() =>
            calendarEvent.Update(
                "Updated",
                "Updated description",
                start,
                start.AddHours(1),
                [new AttendeeDetails("Alex Patient", "alex@example.com", true)]));

        Assert.Equal("A cancelled event cannot be updated.", exception.Message);
    }

    [Fact]
    public void Cancel_is_idempotent()
    {
        var start = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
        var calendarEvent = CalendarEvent.Create(
            "Consultation",
            "Annual review",
            start,
            start.AddMinutes(30),
            [new AttendeeDetails("Alex Patient", "alex@example.com", false)]);

        calendarEvent.Cancel();
        calendarEvent.Cancel();

        Assert.True(calendarEvent.IsCancelled);
    }
}
