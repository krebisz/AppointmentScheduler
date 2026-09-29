using System.Net.Mail;

namespace AppointmentScheduler.Domain.Events;

public sealed class CalendarEvent
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2_000;
    public const int AttendeeNameMaxLength = 200;
    public const int EmailAddressMaxLength = 320;

    private readonly List<Attendee> _attendees = [];

    private CalendarEvent()
    {
    }

    private CalendarEvent(
        Guid id,
        string title,
        string description,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IEnumerable<Attendee> attendees)
    {
        Id = id;
        Title = title;
        Description = description;
        StartTime = startTime;
        EndTime = endTime;
        _attendees.AddRange(attendees);
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset EndTime { get; private set; }

    public IReadOnlyCollection<Attendee> Attendees => _attendees;

    public static CalendarEvent Create(
        string title,
        string description,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IEnumerable<AttendeeDetails> attendees)
    {
        var normalizedTitle = RequiredText(title, nameof(title), TitleMaxLength);
        var normalizedDescription = RequiredText(
            description,
            nameof(description),
            DescriptionMaxLength);

        var utcStart = startTime.ToUniversalTime();
        var utcEnd = endTime.ToUniversalTime();
        if (utcEnd <= utcStart)
        {
            throw new DomainValidationException("End time must be later than start time.");
        }

        var attendeeList = attendees?
            .Select(Attendee.Create)
            .ToList()
            ?? throw new DomainValidationException("At least one attendee is required.");

        if (attendeeList.Count == 0)
        {
            throw new DomainValidationException("At least one attendee is required.");
        }

        if (attendeeList
            .GroupBy(attendee => attendee.EmailAddress, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            throw new DomainValidationException("Attendee email addresses must be unique.");
        }

        return new CalendarEvent(
            Guid.NewGuid(),
            normalizedTitle,
            normalizedDescription,
            utcStart,
            utcEnd,
            attendeeList);
    }

    internal static string RequiredText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainValidationException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}

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
}

public sealed record AttendeeDetails(
    string Name,
    string EmailAddress,
    bool IsAttending);
