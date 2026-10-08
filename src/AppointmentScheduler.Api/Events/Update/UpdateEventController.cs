using AppointmentScheduler.Application.Events.Update;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Api.Events.Update;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class UpdateEventController(UpdateEventHandler updateEventHandler) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UpdateEventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateEventResponse>> Update(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Attendees.Any(attendee => attendee is null))
        {
            ModelState.AddModelError("attendees", "Attendees must not contain null entries.");
            return ValidationProblem(ModelState);
        }

        var result = await updateEventHandler.HandleAsync(
            new UpdateEventCommand(
                id,
                request.Version,
                request.Title,
                request.Description,
                request.StartTime,
                request.EndTime,
                request.Attendees
                    .Select(attendee => new UpdateAttendeeCommand(
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()),
            cancellationToken);

        return Ok(new UpdateEventResponse(
            result.Id,
            result.Title,
            result.Description,
            result.StartTime,
            result.EndTime,
            result.Version,
            result.Attendees
                .Select(attendee => new UpdatedAttendeeResponse(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray()));
    }
}
