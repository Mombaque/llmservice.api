namespace LlmService.Api.Providers;

public class LlmProviderFactory(ILlmProviderClient providerClient)
{
    public ILlmProviderClient GetProvider() => providerClient;
}
