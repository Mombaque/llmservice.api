using LlmService.Api.Contracts;
using LlmService.Api.Middleware;
using LlmService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LlmService.Api.Controllers;

[ApiController]
[Route("v1/embeddings")]
public class EmbeddingsController(LlmEmbeddingService embeddingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<EmbeddingResponse>> Create(
        [FromBody] EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Inputs.Count == 0 || request.Inputs.Any(string.IsNullOrWhiteSpace))
            return BadRequest(new LlmErrorResponse
            {
                Error = new LlmError
                {
                    Code = "invalid_request",
                    Message = "At least one non-empty input is required.",
                    RequestId = GetRequestId()
                }
            });

        var response = await embeddingService.CreateAsync(request, GetRequestId(), cancellationToken);
        return Ok(response);
    }

    private string GetRequestId() =>
        HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId)
            ? correlationId?.ToString() ?? HttpContext.TraceIdentifier
            : HttpContext.TraceIdentifier;
}
