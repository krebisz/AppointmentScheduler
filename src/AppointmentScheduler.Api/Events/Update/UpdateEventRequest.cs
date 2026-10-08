using System.ComponentModel.DataAnnotations;
using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Api.Events.Update;

public sealed record UpdateEventRequest(
    [param: Range(1, long.MaxValue)]
    long Version,
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
    IReadOnlyCollection<UpdateAttendeeRequest> Attendees);
