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
}
