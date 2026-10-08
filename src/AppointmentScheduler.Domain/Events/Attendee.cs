using System.Net.Mail;

namespace AppointmentScheduler.Domain.Events;

public sealed class Attendee
{
    private Attendee()
    {
    }

    private Attendee(Guid id, string name, string emailAddress, bool isAttending)
    {
        Id = id;
        Name = name;
        EmailAddress = emailAddress;
        IsAttending = isAttending;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string EmailAddress { get; private set; } = string.Empty;

    public bool IsAttending { get; private set; }

    internal static Attendee Create(AttendeeDetails details)
    {
        var name = CalendarEvent.RequiredText(
            details.Name,
            nameof(details.Name),
            CalendarEvent.AttendeeNameMaxLength);
        var emailAddress = CalendarEvent.RequiredText(
            details.EmailAddress,
            nameof(details.EmailAddress),
            CalendarEvent.EmailAddressMaxLength);

        if (!MailAddress.TryCreate(emailAddress, out var parsedAddress)
            || !string.Equals(parsedAddress.Address, emailAddress, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("A valid attendee email address is required.");
        }

        return new Attendee(Guid.NewGuid(), name, emailAddress, details.IsAttending);
    }

    internal void SetAttendance(bool isAttending)
    {
        IsAttending = isAttending;
    }
}
