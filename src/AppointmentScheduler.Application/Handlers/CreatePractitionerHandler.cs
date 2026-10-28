
using AppointmentScheduler.Application.Interfaces;
using AppointmentScheduler.Application.Commands;
using AppointmentScheduler.Application.Results;
using AppointmentScheduler.Domain.Entities;

namespace AppointmentScheduler.Application.Handlers;

public sealed class CreatePractitionerHandler(IPractitionerRepository practitionerRepository)
{
    public async Task<CreatePractitionerResult> HandleAsync(CreatePractitionerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var practitionerEvent = new Practitioner(command.FirstName, command.Surname);

        var practitionerResult = await practitionerRepository.CreatePractitioner(practitionerEvent, cancellationToken);

        return practitionerResult;
    }
}