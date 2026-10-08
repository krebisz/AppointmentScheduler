using AppointmentScheduler.Application.Events.Attendance;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Api.Events.Attendance;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class SetAttendanceController(SetAttendanceHandler setAttendanceHandler) : ControllerBase
{
    [HttpPatch("{eventId:guid}/attendees/{attendeeId:guid}/attendance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetAttendance(
        Guid eventId,
        Guid attendeeId,
        SetAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        await setAttendanceHandler.HandleAsync(
            new SetAttendanceCommand(
                eventId,
                attendeeId,
                request.IsAttending!.Value,
                request.Version),
            cancellationToken);

        return NoContent();
    }
}
