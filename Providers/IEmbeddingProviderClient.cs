using LlmService.Api.Contracts;

namespace LlmService.Api.Providers;

public interface IEmbeddingProviderClient
{
    string ProviderName { get; }
    Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, string requestId, CancellationToken cancellationToken);
}
