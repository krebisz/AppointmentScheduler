using AppointmentScheduler.Application.Commands;
using AppointmentScheduler.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Api.Controllers;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CancelEventController(CancelEventHandler cancelEventHandler) : ControllerBase
{
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await cancelEventHandler.HandleAsync(new CancelEventCommand(id), cancellationToken);
        return NoContent();
    }
}
