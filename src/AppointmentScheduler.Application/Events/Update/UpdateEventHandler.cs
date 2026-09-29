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
            command.Id,
            cancellationToken)
            ?? throw new EventNotFoundException(command.Id);

        calendarEvent.Update(
            command.Title,
            command.Description,
            command.StartTime,
            command.EndTime,
            command.Attendees.Select(attendee => new AttendeeDetails(
                attendee.Name,
                attendee.EmailAddress,
                attendee.IsAttending)));

        await eventRepository.SaveChangesAsync(cancellationToken);
        await eventNotificationPublisher.PublishAsync(
            EventNotification.From(calendarEvent, EventNotificationType.Updated),
            cancellationToken);

        return new UpdateEventResult(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.Attendees
                .Select(attendee => new UpdatedAttendeeResult(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray());
    }
}

public sealed record UpdateEventCommand(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<UpdateAttendeeCommand> Attendees);

public sealed record UpdateAttendeeCommand(
    string Name,
    string EmailAddress,
    bool IsAttending);

public sealed record UpdateEventResult(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<UpdatedAttendeeResult> Attendees);

public sealed record UpdatedAttendeeResult(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
