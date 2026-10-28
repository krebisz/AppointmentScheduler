using AppointmentScheduler.Domain.Entities;
using AppointmentScheduler.Application.Inputs;
using AppointmentScheduler.Application.Results;

namespace AppointmentScheduler.Application.Interfaces;

public interface IPractitionerRepository
{
    Task<Practitioner?> GetByIdAsync(Guid Id, CancellationToken cancellationToken);


    Task CreatePractitioner(string FirstName, string Surname, CancellationToken cancellationToken);

    Task<CreatePractitionerResult> CreatePractitioner(Practitioner practitioner, CancellationToken cancellationToken);

    Task<Practitioner> UpdatePractitioner(Guid Id, string FirstName, string Surname, bool IsActive);
}