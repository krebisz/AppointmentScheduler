using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppointmentScheduler.Api.Events.Update;

public sealed class UpdateEventSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(UpdateAttendeeRequest) && schema.Properties is not null)
        {
            if (schema.Properties.TryGetValue("id", out var id))
                id.Description = "Existing attendee ID belonging to this event. Omit or use null to add a new attendee; email does not identify an attendee.";
            if (schema.Properties.TryGetValue("isAttending", out var attendance))
                attendance.Description = "Omit or use null to preserve an existing attendee's response, or default a new attendee to false. Explicit true/false replaces it.";
        }
        if (context.JsonTypeInfo.Type == typeof(UpdateEventRequest) && schema.Properties is not null)
        {
            if (schema.Properties.TryGetValue("attendees", out var attendees))
                attendees.Description = "Complete replacement list: omitted existing attendees are removed. Supply IDs to retain attendees; omitted IDs mean additions.";
            if (schema.Properties.TryGetValue("version", out var version))
                version.Description = "Expected current event version. A stale version returns 409 without saving or publishing.";
        }
        return Task.CompletedTask;
    }
}
