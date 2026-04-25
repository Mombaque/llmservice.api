using LlmService.Api.Contracts;

namespace LlmService.Api.Providers;

public interface ILlmProviderClient
{
    string ProviderName { get; }
    Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, string requestId, CancellationToken cancellationToken);
}
