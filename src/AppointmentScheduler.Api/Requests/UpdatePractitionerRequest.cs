using AppointmentScheduler.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Api.Requests;
public sealed record UpdatePractitionerRequest([param: Required]
    [param: StringLength(Practitioner.FirstNameMaxLength)]
    string FirstName,
    [param: Required]
    [param: StringLength(Practitioner.SurnameMaxLength)]
    string Surname,
    bool IsActive);