using System.ComponentModel.DataAnnotations;
using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Api.Events.Update;

public sealed record UpdateAttendeeRequest(
    [param: Required]
    [param: StringLength(CalendarEvent.AttendeeNameMaxLength)]
    string Name,
    [param: Required]
    [param: StringLength(CalendarEvent.EmailAddressMaxLength)]
    [param: EmailAddress]
    string EmailAddress,
    bool IsAttending);
