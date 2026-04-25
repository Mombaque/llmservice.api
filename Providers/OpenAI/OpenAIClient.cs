using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlmService.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Providers.OpenAI;

public class OpenAIClient(
    HttpClient httpClient,
    IOptions<ResilienceOptions> resilienceOptions,
    ILogger<OpenAIClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<OpenAIResponse> CompleteAsync(OpenAIRequest request, string requestId, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(0, resilienceOptions.Value.RetryCount) + 1;
        var baseDelayMs = Math.Max(0, resilienceOptions.Value.RetryBaseDelayMs);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var response = await httpClient.PostAsJsonAsync("chat/completions", request, JsonOptions, cancellationToken);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "OpenAI request succeeded RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs} StatusCode={StatusCode}",
                        requestId,
                        attempt,
                        stopwatch.ElapsedMilliseconds,
                        (int)response.StatusCode);

                    return await response.Content.ReadFromJsonAsync<OpenAIResponse>(cancellationToken)
                        ?? throw new LlmProviderException("llm_provider_empty_response", "LLM provider returned an empty response.", StatusCodes.Status502BadGateway);
                }

                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "OpenAI request failed RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs} StatusCode={StatusCode} Error={Error}",
                    requestId,
                    attempt,
                    stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode,
                    error);

                if (!ShouldRetry(response.StatusCode) || attempt == attempts)
                    throw new LlmProviderException("llm_provider_error", "LLM provider request failed.", StatusCodes.Status502BadGateway);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                logger.LogWarning(
                    ex,
                    "OpenAI request timed out RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs}",
                    requestId,
                    attempt,
                    stopwatch.ElapsedMilliseconds);

                if (attempt == attempts)
                    throw new LlmProviderException("llm_provider_timeout", "LLM provider request timed out.", StatusCodes.Status408RequestTimeout, ex);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                logger.LogWarning(
                    ex,
                    "OpenAI request transport error RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs}",
                    requestId,
                    attempt,
                    stopwatch.ElapsedMilliseconds);

                if (attempt == attempts)
                    throw new LlmProviderException("llm_provider_unavailable", "LLM provider is unavailable.", StatusCodes.Status503ServiceUnavailable, ex);
            }

            var delayMs = baseDelayMs * attempt;
            if (delayMs > 0)
                await Task.Delay(delayMs, cancellationToken);
        }

        throw new LlmProviderException("llm_provider_error", "LLM provider request failed.", StatusCodes.Status502BadGateway);
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
