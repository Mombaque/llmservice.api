using LlmService.Api.Contracts;
using LlmService.Api.Middleware;
using LlmService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LlmService.Api.Controllers;

[ApiController]
[Route("v1/chat/completions")]
public class ChatCompletionsController(LlmCompletionService completionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatCompletionResponse>> Complete(
        [FromBody] ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Messages.Count == 0)
            return BadRequest(new LlmErrorResponse
            {
                Error = new LlmError
                {
                    Code = "invalid_request",
                    Message = "At least one message is required.",
                    RequestId = GetRequestId()
                }
            });

        var response = await completionService.CompleteAsync(request, GetRequestId(), cancellationToken);
        return Ok(response);
    }

    private string GetRequestId() =>
        HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId)
            ? correlationId?.ToString() ?? HttpContext.TraceIdentifier
            : HttpContext.TraceIdentifier;
}
