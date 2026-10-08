using AppointmentScheduler.Application.Events.Persistence;
using AppointmentScheduler.Domain.Events;
using AppointmentScheduler.Application.Events.Notifications;

namespace AppointmentScheduler.Application.Events.Update;

public sealed class UpdateEventHandler(
    IEventRepository eventRepository,
    IEventNotificationPublisher eventNotificationPublisher)
{
    public async Task<UpdateEventResult> HandleAsync(
        UpdateEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await eventRepository.GetByIdAsync(
            command.EventId,
            cancellationToken)
            ?? throw new EventNotFoundException(command.EventId);

        if (command.ExpectedVersion != calendarEvent.Version)
        {
            throw new EventConcurrencyException(
                calendarEvent.Id,
                command.ExpectedVersion,
                calendarEvent.Version);
        }

        calendarEvent.Update(
            command.Title,
            command.Description,
            command.StartTime,
            command.EndTime,
            command.Attendees.Select(attendee => new AttendeeUpdateDetails(
                attendee.Name,
                attendee.EmailAddress,
                attendee.IsAttending,
                attendee.AttendeeId)));

        await eventRepository.SaveChangesAsync(cancellationToken);
        await eventNotificationPublisher.PublishAsync(
            EventNotification.FromEvent(calendarEvent, EventNotificationType.Updated),
            cancellationToken);

        return new UpdateEventResult(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.Version,
            calendarEvent.Attendees
                .Select(attendee => new UpdateAttendeeResult(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray());
    }
}
