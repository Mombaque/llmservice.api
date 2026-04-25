using System.Diagnostics;
using LlmService.Api.Contracts;
using LlmService.Api.Providers;

namespace LlmService.Api.Services;

public class LlmCompletionService(
    LlmProviderFactory providerFactory,
    ILogger<LlmCompletionService> logger)
{
    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        string requestId,
        CancellationToken cancellationToken)
    {
        var provider = providerFactory.GetProvider();
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "LLM completion started RequestId={RequestId} Provider={Provider} Source={Source} ConversationId={ConversationId} MessageCount={MessageCount} ToolCount={ToolCount}",
            requestId,
            provider.ProviderName,
            GetMetadata(request, "source"),
            GetMetadata(request, "conversationId"),
            request.Messages.Count,
            request.Tools?.Count ?? 0);

        var response = await provider.CompleteAsync(request, requestId, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "LLM completion finished RequestId={RequestId} Provider={Provider} Model={Model} StopReason={StopReason} DurationMs={DurationMs} InputTokens={InputTokens} OutputTokens={OutputTokens}",
            requestId,
            response.Provider,
            response.Model,
            response.StopReason,
            stopwatch.ElapsedMilliseconds,
            response.Usage.InputTokens,
            response.Usage.OutputTokens);

        return response;
    }

    private static string? GetMetadata(ChatCompletionRequest request, string key) =>
        request.Metadata is not null && request.Metadata.TryGetValue(key, out var value) ? value : null;
}
