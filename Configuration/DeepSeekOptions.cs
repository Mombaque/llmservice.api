namespace LlmService.Api.Configuration;

public class DeepSeekOptions : IChatCompletionsProviderOptions
{
    public const string Section = "DeepSeek";

    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "deepseek-chat";
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1/";
    public int TimeoutSeconds { get; set; } = 60;
}
