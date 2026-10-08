using AppointmentScheduler.Application.Events.Get;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentScheduler.Api.Events.Get;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class GetEventController(GetEventHandler getEventHandler) : ControllerBase
{
    public const string RouteName = "GetEvent";

    [HttpGet("{id:guid}", Name = RouteName)]
    [ProducesResponseType<GetEventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetEventResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await getEventHandler.HandleAsync(new GetEventQuery(id), cancellationToken);
        return Ok(new GetEventResponse(
            result.Id, result.Title, result.Description, result.StartTime, result.EndTime,
            result.IsCancelled, result.Version,
            result.Attendees.Select(attendee => new GetAttendeeResponse(
                attendee.Id, attendee.Name, attendee.EmailAddress, attendee.IsAttending)).ToArray()));
    }
}
