using System.ComponentModel.DataAnnotations;
using AppointmentScheduler.Domain.Events;

namespace AppointmentScheduler.Api.Events.Create;

public sealed record CreateAttendeeRequest(
    [param: Required]
    [param: StringLength(Attendee.NameMaxLength)]
    string Name,
    [param: Required]
    [param: StringLength(Attendee.EmailAddressMaxLength)]
    [param: EmailAddress]
    string EmailAddress,
    bool IsAttending);
