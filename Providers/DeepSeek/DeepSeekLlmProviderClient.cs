using LlmService.Api.Configuration;
using LlmService.Api.Providers.ChatCompletions;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Providers.DeepSeek;

public class DeepSeekLlmProviderClient(
    IHttpClientFactory httpClientFactory,
    ChatCompletionsProviderClient client,
    IOptions<DeepSeekOptions> options,
    ILogger<DeepSeekLlmProviderClient> logger)
    : ChatCompletionsProviderAdapter<DeepSeekOptions>(httpClientFactory, client, options, logger)
{
    public override string ProviderName => "DeepSeek";
}
