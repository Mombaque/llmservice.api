using LlmService.Api.Configuration;
using LlmService.Api.Providers.ChatCompletions;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Providers.OpenAI;

public class OpenAILlmProviderClient(
    IHttpClientFactory httpClientFactory,
    ChatCompletionsProviderClient client,
    IOptions<OpenAIOptions> options,
    ILogger<OpenAILlmProviderClient> logger)
    : ChatCompletionsProviderAdapter<OpenAIOptions>(httpClientFactory, client, options, logger)
{
    public override string ProviderName => "OpenAI";
}
