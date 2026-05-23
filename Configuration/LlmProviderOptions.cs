namespace LlmService.Api.Configuration;

public class LlmProviderOptions
{
    public const string Section = "LlmProvider";

    public string DefaultProvider { get; set; } = "DeepSeek";
}
