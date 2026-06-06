using System.Diagnostics;
using LlmService.Api.Configuration;
using LlmService.Api.Contracts;
using LlmService.Api.Providers.Embeddings;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Providers.OpenAI;

public class OpenAIEmbeddingProviderClient(
    IHttpClientFactory httpClientFactory,
    EmbeddingsProviderClient client,
    IOptions<OpenAIOptions> options,
    ILogger<OpenAIEmbeddingProviderClient> logger) : IEmbeddingProviderClient
{
    public string ProviderName => "OpenAI";

    public async Task<EmbeddingResponse> CreateEmbeddingAsync(
        EmbeddingRequest request,
        string requestId,
        CancellationToken cancellationToken)
    {
        var model = request.Model ?? options.Value.DefaultEmbeddingModel;
        if (string.IsNullOrWhiteSpace(model))
            throw new LlmProviderException("llm_model_not_configured", "OpenAI embedding model is not configured.", StatusCodes.Status500InternalServerError);

        var providerRequest = new EmbeddingsProviderRequest
        {
            Model = model,
            Input = request.Inputs
        };

        var stopwatch = Stopwatch.StartNew();
        var httpClient = httpClientFactory.CreateClient(ProviderName);
        var providerResponse = await client.CreateAsync(httpClient, providerRequest, requestId, ProviderName, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "LLM embedding normalized RequestId={RequestId} Provider={Provider} Model={Model} DurationMs={DurationMs} InputTokens={InputTokens} TotalTokens={TotalTokens}",
            requestId,
            ProviderName,
            model,
            stopwatch.ElapsedMilliseconds,
            providerResponse.Usage.PromptTokens,
            providerResponse.Usage.TotalTokens);

        return new EmbeddingResponse
        {
            Provider = ProviderName,
            Model = model,
            RequestId = requestId,
            Embeddings = providerResponse.Data
                .OrderBy(item => item.Index)
                .Select(item => new EmbeddingDataDto
                {
                    Index = item.Index,
                    Vector = item.Embedding
                })
                .ToArray(),
            Usage = new LlmUsageDto
            {
                InputTokens = providerResponse.Usage.PromptTokens,
                OutputTokens = 0
            }
        };
    }
}
