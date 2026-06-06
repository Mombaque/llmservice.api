namespace LlmService.Api.Contracts;

public class EmbeddingRequest
{
    public List<string> Inputs { get; set; } = [];
    public Dictionary<string, string>? Metadata { get; set; }
}
