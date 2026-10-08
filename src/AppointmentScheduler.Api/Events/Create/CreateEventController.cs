using AppointmentScheduler.Application.Events.Create;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Api.Events.Create;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CreateEventController(CreateEventHandler createEventHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateEventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEventResponse>> Create(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Attendees.Any(attendee => attendee is null))
        {
            ModelState.AddModelError("attendees", "Attendees must not contain null entries.");
            return ValidationProblem(ModelState);
        }

        var result = await createEventHandler.HandleAsync(
            new CreateEventCommand(
                request.Title,
                request.Description,
                request.StartTime,
                request.EndTime,
                request.Attendees
                    .Select(attendee => new CreateAttendeeCommand(
                        attendee.Name,
                        attendee.EmailAddress,
                        attendee.IsAttending))
                    .ToArray()),
            cancellationToken);

        var response = new CreateEventResponse(
            result.Id,
            result.Title,
            result.Description,
            result.StartTime,
            result.EndTime,
            result.Version,
            result.Attendees
                .Select(attendee => new CreatedAttendeeResponse(
                    attendee.Id,
                    attendee.Name,
                    attendee.EmailAddress,
                    attendee.IsAttending))
                .ToArray());

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
