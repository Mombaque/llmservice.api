namespace LlmService.Api.Configuration;

public class OpenAIOptions
{
    public const string Section = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public int TimeoutSeconds { get; set; } = 60;
}
