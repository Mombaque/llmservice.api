using System.Text.Json.Serialization;

namespace LlmService.Api.Providers.Embeddings;

public class EmbeddingsProviderResponse
{
    [JsonPropertyName("data")]
    public List<EmbeddingsProviderData> Data { get; set; } = [];

    [JsonPropertyName("usage")]
    public EmbeddingsProviderUsage Usage { get; set; } = new();
}

public class EmbeddingsProviderData
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = [];
}

public class EmbeddingsProviderUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
