using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppointmentScheduler.Api.OpenApi;

public sealed class PractitionerOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.HttpMethod == "POST" && context.Description.RelativePath == "api/practitioner"
            && operation.Responses?.TryGetValue("201", out var response) == true
            && response is OpenApiResponse createdResponse)
        {
            createdResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            createdResponse.Headers["Location"] = new OpenApiHeader
            {
                Description = "URL of the registered practitioner; GET returns its current details, including activation state.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" }
            };
        }
        return Task.CompletedTask;
    }
}
