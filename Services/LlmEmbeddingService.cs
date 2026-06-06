using System.Diagnostics;
using LlmService.Api.Contracts;
using LlmService.Api.Providers;

namespace LlmService.Api.Services;

public class LlmEmbeddingService(
    EmbeddingProviderFactory providerFactory,
    ILogger<LlmEmbeddingService> logger)
{
    public async Task<EmbeddingResponse> CreateAsync(
        EmbeddingRequest request,
        string requestId,
        CancellationToken cancellationToken)
    {
        var provider = providerFactory.GetProvider();
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "LLM embedding started RequestId={RequestId} Provider={Provider} Source={Source} InputCount={InputCount}",
            requestId,
            provider.ProviderName,
            GetMetadata(request, "source"),
            request.Inputs.Count);

        var response = await provider.CreateEmbeddingAsync(request, requestId, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "LLM embedding finished RequestId={RequestId} Provider={Provider} Model={Model} DurationMs={DurationMs} InputTokens={InputTokens} EmbeddingCount={EmbeddingCount}",
            requestId,
            response.Provider,
            response.Model,
            stopwatch.ElapsedMilliseconds,
            response.Usage.InputTokens,
            response.Embeddings.Length);

        return response;
    }

    private static string? GetMetadata(EmbeddingRequest request, string key) =>
        request.Metadata is not null && request.Metadata.TryGetValue(key, out var value) ? value : null;
}
