using AppointmentScheduler.Api.Requests;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppointmentScheduler.Api.OpenApi;

public sealed class UpdatePractitionerSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(UpdatePractitionerRequest) && schema.Properties is not null)
        {
            if (schema.Properties.TryGetValue("id", out var id))
                id.Description = "Existing practitioner ID already exists. Omit or use null to add a new practitioner; First name and Surname does not identify an attendee.";
        }

        return Task.CompletedTask;
    }
}
