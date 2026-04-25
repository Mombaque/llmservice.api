using System.Text.Json;

namespace LlmService.Api.Contracts;

public class LlmContentBlockDto
{
    public string Type { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public JsonDocument? ToolInput { get; set; }
}
