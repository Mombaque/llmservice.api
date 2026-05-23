using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlmService.Api.Configuration;
using LlmService.Api.Providers;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Providers.ChatCompletions;

public class ChatCompletionsProviderClient(
    IOptions<ResilienceOptions> resilienceOptions,
    ILogger<ChatCompletionsProviderClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<ChatCompletionsProviderResponse> CompleteAsync(
        HttpClient httpClient,
        ChatCompletionsProviderRequest request,
        string requestId,
        string providerName,
        CancellationToken cancellationToken)
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
                        "{Provider} request succeeded RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs} StatusCode={StatusCode}",
                        providerName,
                        requestId,
                        attempt,
                        stopwatch.ElapsedMilliseconds,
                        (int)response.StatusCode);

                    return await response.Content.ReadFromJsonAsync<ChatCompletionsProviderResponse>(cancellationToken)
                        ?? throw new LlmProviderException("llm_provider_empty_response", "LLM provider returned an empty response.", StatusCodes.Status502BadGateway);
                }

                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "{Provider} request failed RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs} StatusCode={StatusCode} Error={Error}",
                    providerName,
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
                    "{Provider} request timed out RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs}",
                    providerName,
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
                    "{Provider} request transport error RequestId={RequestId} Attempt={Attempt} DurationMs={DurationMs}",
                    providerName,
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
