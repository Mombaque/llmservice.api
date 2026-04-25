using System.Text.Json;

namespace LlmService.Api.Contracts;

public class LlmToolDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonDocument InputSchema { get; set; } = JsonDocument.Parse("{}");
}
