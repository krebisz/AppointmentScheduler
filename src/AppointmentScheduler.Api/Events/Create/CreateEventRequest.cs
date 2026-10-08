using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Api.Events.Create;

public sealed record CreateEventRequest(
    [param: Required]
    [param: StringLength(CalendarEvent.TitleMaxLength)]
    string Title,
    [param: Required]
    [param: StringLength(CalendarEvent.DescriptionMaxLength)]
    string Description,
    // Require timestamp presence in JSON so omission cannot become a default DateTimeOffset.
    [property: JsonRequired]
    DateTimeOffset StartTime,
    [property: JsonRequired]
    DateTimeOffset EndTime,
    [param: Required]
    [param: MinLength(1)]
    IReadOnlyCollection<CreateAttendeeRequest> Attendees);
