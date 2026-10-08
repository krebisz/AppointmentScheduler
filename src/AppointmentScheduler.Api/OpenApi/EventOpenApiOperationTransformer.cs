using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppointmentScheduler.Api.OpenApi;

public sealed class EventOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.HttpMethod == "POST" && context.Description.RelativePath == "api/events"
            && operation.Responses?.TryGetValue("201", out var response) == true
            && response is OpenApiResponse createdResponse)
        {
            createdResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            createdResponse.Headers["Location"] = new OpenApiHeader
            {
                Description = "URL of the created event; GET returns its current details, including cancellation state.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" }
            };
        }
        return Task.CompletedTask;
    }
}
