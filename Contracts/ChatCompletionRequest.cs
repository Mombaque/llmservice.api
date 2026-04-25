namespace LlmService.Api.Contracts;

public class ChatCompletionRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public List<LlmMessageDto> Messages { get; set; } = [];
    public List<LlmToolDto>? Tools { get; set; }
    public int MaxTokens { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
