namespace LlmService.Api.Configuration;

public class ResilienceOptions
{
    public const string Section = "Resilience";

    public int RetryCount { get; set; } = 2;
    public int RetryBaseDelayMs { get; set; } = 300;
}
