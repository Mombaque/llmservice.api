using LlmService.Api.Contracts;
using LlmService.Api.Providers.OpenAI;

namespace LlmService.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (LlmProviderException ex)
        {
            var requestId = GetRequestId(context);
            logger.LogError(ex, "LLM provider error RequestId={RequestId} Code={Code} StatusCode={StatusCode}", requestId, ex.Code, ex.StatusCode);
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new LlmErrorResponse
            {
                Error = new LlmError
                {
                    Code = ex.Code,
                    Message = ex.Message,
                    RequestId = requestId
                }
            });
        }
        catch (Exception ex)
        {
            var requestId = GetRequestId(context);
            logger.LogError(ex, "Unhandled LLM service error RequestId={RequestId}", requestId);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new LlmErrorResponse
            {
                Error = new LlmError
                {
                    Code = "llm_service_error",
                    Message = "LLM service failed unexpectedly.",
                    RequestId = requestId
                }
            });
        }
    }

    private static string GetRequestId(HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId)
            ? correlationId?.ToString() ?? context.TraceIdentifier
            : context.TraceIdentifier;
}
