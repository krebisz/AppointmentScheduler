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
        Version = 1;
        _attendees.AddRange(attendees);
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset EndTime { get; private set; }

    public bool IsCancelled { get; private set; }

    public long Version { get; private set; }

    public IReadOnlyCollection<Attendee> Attendees => _attendees;

    public static CalendarEvent Create(
        string title,
        string description,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IEnumerable<AttendeeDetails> attendees)
    {
        var details = ValidateDetails(
            title,
            description,
            startTime,
            endTime,
            attendees);

        return new CalendarEvent(
            Guid.NewGuid(),
            details.Title,
            details.Description,
            details.StartTime,
            details.EndTime,
            details.Attendees);
    }

    public void Update(
        string title,
        string description,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IEnumerable<AttendeeDetails> attendees)
    {
        if (IsCancelled)
        {
            throw new DomainValidationException("A cancelled event cannot be updated.");
        }

        var details = ValidateDetails(
            title,
            description,
            startTime,
            endTime,
            attendees);

        Title = details.Title;
        Description = details.Description;
        StartTime = details.StartTime;
        EndTime = details.EndTime;
        _attendees.Clear();
        _attendees.AddRange(details.Attendees);
        Version++;
    }

    public bool Cancel()
    {
        if (IsCancelled)
        {
            return false;
        }

        IsCancelled = true;
        Version++;
        return true;
    }

    public bool SetAttendance(Guid attendeeId, bool isAttending)
    {
        if (IsCancelled)
        {
            throw new DomainValidationException(
                "Attendance cannot be changed for a cancelled event.");
        }

        var attendee = _attendees.SingleOrDefault(item => item.Id == attendeeId)
            ?? throw new DomainValidationException(
                "The attendee does not belong to this event.");

        if (attendee.IsAttending == isAttending)
        {
            return false;
        }

        attendee.SetAttendance(isAttending);
        Version++;
        return true;
    }

    private static ValidatedEventDetails ValidateDetails(
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

        return new ValidatedEventDetails(
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

    private sealed record ValidatedEventDetails(
        string Title,
        string Description,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        IReadOnlyCollection<Attendee> Attendees);
}
