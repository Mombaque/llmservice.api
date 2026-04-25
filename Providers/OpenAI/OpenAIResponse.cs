using System.Text.Json.Serialization;

namespace LlmService.Api.Providers.OpenAI;

public class OpenAIResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<OpenAIChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public OpenAIUsage Usage { get; set; } = new();
}

public class OpenAIChoice
{
    [JsonPropertyName("message")]
    public OpenAIMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}

public class OpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}
