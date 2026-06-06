using System.Text.Json.Serialization;

namespace LlmService.Api.Providers.Embeddings;

public class EmbeddingsProviderRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public List<string> Input { get; set; } = [];
}
