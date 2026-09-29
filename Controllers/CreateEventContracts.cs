using System.ComponentModel.DataAnnotations;
using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Controllers;

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

public sealed record CreateAttendeeRequest(
    [param: Required]
    [param: StringLength(CalendarEvent.AttendeeNameMaxLength)]
    string Name,
    [param: Required]
    [param: StringLength(CalendarEvent.EmailAddressMaxLength)]
    [param: EmailAddress]
    string EmailAddress,
    bool IsAttending);

public sealed record CreateEventResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<CreatedAttendeeResponse> Attendees);

public sealed record CreatedAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);

public sealed record EventListItemResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyCollection<EventAttendeeResponse> Attendees);

public sealed record EventAttendeeResponse(
    Guid Id,
    string Name,
    string EmailAddress,
    bool IsAttending);
