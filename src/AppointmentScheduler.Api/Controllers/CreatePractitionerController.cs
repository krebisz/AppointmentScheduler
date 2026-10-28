using AppointmentScheduler.Application.Commands;
using AppointmentScheduler.Application.Handlers;
using AppointmentScheduler.Application.Results;
using Microsoft.AspNetCore.Mvc;


namespace AppointmentScheduler.Api.Controllers;

[ApiController]
[Route("api/practitioner")]
[Tags("Practitioner")]
public class CreatePractitionerController(CreatePractitionerHandler createPractitionerHandler) : ControllerBase
{
    private CancellationToken cancellationToken;

    [HttpPost]
    public async Task<IActionResult> CreatePractitioner(string FirstName, string Surname, bool IsActive)
    {
        CreatePractitionerCommand command = new CreatePractitionerCommand(FirstName, Surname, IsActive);

            //save details - get resualt
            CreatePractitionerResult result = await createPractitionerHandler.HandleAsync(command, cancellationToken);
            
        return Ok(result); 
    }
}