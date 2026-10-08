using AppointmentScheduler.Application.Events.Persistence;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Application.Events.Notifications;

namespace AppointmentScheduler.Application.Events.Create;

public sealed class CreateEventHandler(
    IEventRepository eventRepository,
    IEventNotificationPublisher eventNotificationPublisher)
{
    public async Task<CreateEventResult> HandleAsync(
        CreateEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = CalendarEvent.Create(
            command.Title,
            command.Description,
            command.StartTime,
            command.EndTime,
            command.Attendees.Select(attendee => new AttendeeDetails(
                attendee.Name,
                attendee.EmailAddress,
                attendee.IsAttending)));

        await eventRepository.AddAsync(calendarEvent, cancellationToken);
        await eventNotificationPublisher.PublishAsync(
            EventNotification.From(calendarEvent, EventNotificationType.Created),
            cancellationToken);

        return new CreateEventResult(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.Version,
            calendarEvent.Attendees
                .Select(attendee => new CreatedAttendeeResult(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray());
    }
}
