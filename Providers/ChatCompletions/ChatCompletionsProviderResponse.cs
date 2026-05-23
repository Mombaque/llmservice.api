using System.Text.Json.Serialization;

namespace LlmService.Api.Providers.ChatCompletions;

public class ChatCompletionsProviderResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ChatCompletionsProviderChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public ChatCompletionsProviderUsage Usage { get; set; } = new();
}

public class ChatCompletionsProviderChoice
{
    [JsonPropertyName("message")]
    public ChatCompletionsProviderMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}

public class ChatCompletionsProviderUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}
