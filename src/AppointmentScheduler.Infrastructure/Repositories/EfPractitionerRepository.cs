
using AppointmentScheduler.Domain.Entities;
using AppointmentScheduler.Domain.Inputs;
using AppointmentScheduler.Application.Interfaces;
using AppointmentScheduler.Application.Results;
using AppointmentScheduler.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;


namespace AppointmentScheduler.Infrastructure.Repositories;

public sealed class EfPractitionerRepository(SchedulerDbContext dbContext) : IPractitionerRepository
{
    public async Task<Practitioner?> GetByIdAsync(Guid Id, CancellationToken cancellationToken)
    {
        var result = await dbContext.Practitioners.Include(x => x.ID == Id).SingleOrDefaultAsync();
        return result;
    }

    public async Task CreatePractitioner(string FirstName, string Surname, CancellationToken cancellationToken)
    {
        Practitioner practitioner = new Practitioner(FirstName, Surname);
        await dbContext.Practitioners.AddAsync(practitioner, cancellationToken);
        //return praactitioner;
    }

    public Task<CreatePractitionerResult> CreatePractitioner(Practitioner practitioner,
        CancellationToken cancellationToken)
    {
        return null;
    }

    //NEED TO HAVE ERROR BUBBLE UP IF PRACTITION IS NULL
    public async Task<Practitioner?> UpdatePractitioner(Guid Id, string FirstName, string Surname, bool IsActive)
    {
        Practitioner? updatedPractitioner = null;
        Practitioner? practitioner = await dbContext.Practitioners.Include(x => x.ID == Id).SingleOrDefaultAsync();

        if (practitioner is not null)
        {
            PractitionerDetails practitionerDetails = new PractitionerDetails(practitioner.ID, practitioner.FirstName,
                practitioner.Surname, practitioner.IsActive);
            updatedPractitioner = practitioner.Update(practitionerDetails);
        }

        return updatedPractitioner;
    }
}