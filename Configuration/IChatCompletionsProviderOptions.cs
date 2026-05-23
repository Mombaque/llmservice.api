namespace LlmService.Api.Configuration;

public interface IChatCompletionsProviderOptions
{
    string ApiKey { get; set; }
    string DefaultModel { get; set; }
    string BaseUrl { get; set; }
    int TimeoutSeconds { get; set; }
}
