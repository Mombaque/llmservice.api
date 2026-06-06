namespace LlmService.Api.Contracts;

public class EmbeddingResponse
{
    public EmbeddingDataDto[] Embeddings { get; set; } = [];
    public LlmUsageDto Usage { get; set; } = new();
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}

public class EmbeddingDataDto
{
    public int Index { get; set; }
    public float[] Vector { get; set; } = [];
}
