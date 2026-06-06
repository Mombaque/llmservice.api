namespace LlmService.Api.Configuration;

public class OpenAIOptions : IChatCompletionsProviderOptions
{
    public const string Section = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public string DefaultEmbeddingModel { get; set; } = "text-embedding-3-small";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public int TimeoutSeconds { get; set; } = 60;
}
