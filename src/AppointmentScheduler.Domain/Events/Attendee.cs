using System.Net.Mail;
using AppointmentScheduler.Domain.Events.Validation;

namespace AppointmentScheduler.Domain.Events;

public sealed class Attendee
{
    public const int NameMaxLength = 200;
    public const int EmailAddressMaxLength = 320;

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
        var name = RequiredTextValidator.ValidateAndNormalize(
            details.Name,
            nameof(details.Name),
            NameMaxLength);
        var emailAddress = RequiredTextValidator.ValidateAndNormalize(
            details.EmailAddress,
            nameof(details.EmailAddress),
            EmailAddressMaxLength);

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

    internal static Attendee CreateReplacement(AttendeeUpdateDetails details, Attendee? existing)
    {
        // Retain existing IDs and omitted attendance; additions get new IDs and default to false.
        var replacement = Create(new AttendeeDetails(
            details.Name, details.EmailAddress, details.IsAttending ?? existing?.IsAttending ?? false));
        if (existing is not null) replacement.Id = existing.Id;
        return replacement;
    }

    internal void ApplyReplacement(Attendee replacement)
    {
        Name = replacement.Name;
        EmailAddress = replacement.EmailAddress;
        IsAttending = replacement.IsAttending;
    }
}
