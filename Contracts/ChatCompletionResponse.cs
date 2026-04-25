namespace LlmService.Api.Contracts;

public class ChatCompletionResponse
{
    public string StopReason { get; set; } = string.Empty;
    public LlmContentBlockDto[] Content { get; set; } = [];
    public LlmUsageDto Usage { get; set; } = new();
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}
