using System.ComponentModel.DataAnnotations;
using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Api.Events.Create;

public sealed record CreateEventRequest(
    [param: Required]
    [param: StringLength(CalendarEvent.TitleMaxLength)]
    string Title,
    [param: Required]
    [param: StringLength(CalendarEvent.DescriptionMaxLength)]
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    [param: Required]
    [param: MinLength(1)]
    IReadOnlyCollection<CreateAttendeeRequest> Attendees);
