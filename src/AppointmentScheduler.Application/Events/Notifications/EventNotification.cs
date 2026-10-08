using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Application.Events.Notifications;

public sealed record EventNotification(
    EventNotificationType Type,
    Guid EventId,
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<string> RecipientEmailAddresses)
{
    public static EventNotification From(
        CalendarEvent calendarEvent,
        EventNotificationType type)
    {
        return new EventNotification(
            type,
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.Attendees
                .Select(attendee => attendee.EmailAddress)
                .ToArray());
    }
}
