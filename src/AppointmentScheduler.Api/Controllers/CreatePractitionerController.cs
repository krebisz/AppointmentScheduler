using AppointmentScheduler.Application.Handlers;
using Microsoft.AspNetCore.Mvc;


namespace AppointmentScheduler.Api.Controllers;

[ApiController]
[Route("api/practitioner")]
[Tags("Practitioner")]
public class CreatePractitionerController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePractitioner
}