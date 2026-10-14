using AppointmentScheduler.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Api.Requests;

public sealed record UpdateAttendeeRequest(
    [param: Required]
    [param: StringLength(Attendee.NameMaxLength)]
    string Name,
    [param: Required]
    [param: StringLength(Attendee.EmailAddressMaxLength)]
    [param: EmailAddress]
    string EmailAddress,
    bool? IsAttending = null,
    Guid? Id = null);
