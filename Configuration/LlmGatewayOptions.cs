namespace LlmService.Api.Configuration;

public class LlmGatewayOptions
{
    public const string Section = "LlmGateway";

    public string InternalApiKey { get; set; } = string.Empty;
}
