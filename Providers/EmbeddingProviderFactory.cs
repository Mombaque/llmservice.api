namespace LlmService.Api.Providers;

public class EmbeddingProviderFactory(IEmbeddingProviderClient providerClient)
{
    public IEmbeddingProviderClient GetProvider() => providerClient;
}
