namespace LlmService.Api.Contracts;

public class LlmMessageDto
{
    public string Role { get; set; } = string.Empty;
    public LlmContentBlockDto[] Content { get; set; } = [];
}
