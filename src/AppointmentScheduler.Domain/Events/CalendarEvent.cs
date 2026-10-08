using AppointmentScheduler.Domain.Events.Validation;

namespace AppointmentScheduler.Domain.Events;

public sealed class CalendarEvent
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2_000;

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
            attendees?.Select(Attendee.Create));

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
        IEnumerable<AttendeeUpdateDetails> attendees)
    {
        if (IsCancelled)
        {
            throw new DomainValidationException("A cancelled event cannot be updated.");
        }

        var existingAttendees = _attendees.ToDictionary(attendee => attendee.Id);
        var suppliedIds = new HashSet<Guid>();
        var replacements = attendees?.Select(replacement =>
        {
            if (replacement is null)
                throw new DomainValidationException("Attendees must not contain null entries.");
            Attendee? existing = null;
            if (replacement.AttendeeId is Guid attendeeId)
            {
                if (!suppliedIds.Add(attendeeId))
                    throw new DomainValidationException("Attendee IDs must be unique.");
                if (!existingAttendees.TryGetValue(attendeeId, out existing))
                    throw new DomainValidationException("The attendee does not belong to this event.");
            }
            return Attendee.CreateReplacement(replacement, existing);
        });

        var details = ValidateDetails(
            title,
            description,
            startTime,
            endTime,
            replacements);

        // All replacements and event fields are now valid. Retain tracked instances for existing IDs.
        var reconciledAttendees = details.Attendees.Select(replacement =>
        {
            if (!existingAttendees.TryGetValue(replacement.Id, out var existing)) return replacement;
            existing.ApplyReplacement(replacement);
            return existing;
        }).ToArray();

        Title = details.Title;
        Description = details.Description;
        StartTime = details.StartTime;
        EndTime = details.EndTime;
        _attendees.Clear();
        _attendees.AddRange(reconciledAttendees);
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
        IEnumerable<Attendee>? attendees)
    {
        var normalizedTitle = RequiredTextValidator.ValidateAndNormalize(title, nameof(title), TitleMaxLength);
        var normalizedDescription = RequiredTextValidator.ValidateAndNormalize(
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

    private sealed record ValidatedEventDetails(
        string Title,
        string Description,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        IReadOnlyCollection<Attendee> Attendees);
}
