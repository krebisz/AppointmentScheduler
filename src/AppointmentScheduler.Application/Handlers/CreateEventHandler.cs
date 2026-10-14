using AppointmentScheduler.Application.Commands;
using AppointmentScheduler.Application.Interfaces;
using AppointmentScheduler.Application.Notifications;
using AppointmentScheduler.Application.Results;
using AppointmentScheduler.Domain.Entities;
using AppointmentScheduler.Domain.Inputs;

namespace AppointmentScheduler.Application.Handlers;

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

        await eventRepository.AddAndSaveAsync(calendarEvent, cancellationToken);
        await eventNotificationPublisher.PublishAsync(
            EventNotification.FromEvent(calendarEvent, EventNotificationType.Created),
            cancellationToken);

        return new CreateEventResult(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.Version,
            calendarEvent.Attendees
                .Select(attendee => new CreateAttendeeResult(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray());
    }
}
