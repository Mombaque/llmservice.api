using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LlmService.Api.Configuration;
using LlmService.Api.Contracts;
using LlmService.Api.Providers.Embeddings;
using LlmService.Api.Providers.OpenAI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LlmService.Api.Tests;

public class OpenAIEmbeddingProviderClientTests
{
    [Fact]
    public async Task CreateEmbeddingAsync_UsesDefaultEmbeddingModelAndMapsResponse()
    {
        using var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                data = new[]
                {
                    new { index = 0, embedding = new[] { 0.1f, 0.2f } }
                },
                usage = new { prompt_tokens = 3, total_tokens = 3 }
            })
        });
        var provider = CreateProvider(handler);

        var response = await provider.CreateEmbeddingAsync(
            new EmbeddingRequest { Inputs = ["hello"] },
            "request-1",
            CancellationToken.None);

        Assert.Equal("OpenAI", response.Provider);
        Assert.Equal("text-embedding-3-small", response.Model);
        Assert.Equal("request-1", response.RequestId);
        Assert.Equal(3, response.Usage.InputTokens);
        Assert.Single(response.Embeddings);
        Assert.Equal([0.1f, 0.2f], response.Embeddings[0].Vector);

        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("https://api.openai.test/v1/embeddings", handler.Request?.RequestUri?.ToString());

        var payload = JsonDocument.Parse(handler.Body ?? "{}").RootElement;
        Assert.Equal("text-embedding-3-small", payload.GetProperty("model").GetString());
        Assert.Equal("hello", payload.GetProperty("input")[0].GetString());
    }

    private static OpenAIEmbeddingProviderClient CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.test/v1/")
        };

        return new OpenAIEmbeddingProviderClient(
            new SingleClientFactory(httpClient),
            new EmbeddingsProviderClient(
                Options.Create(new ResilienceOptions { RetryCount = 0, RetryBaseDelayMs = 0 }),
                NullLogger<EmbeddingsProviderClient>.Instance),
            Options.Create(new OpenAIOptions
            {
                DefaultEmbeddingModel = "text-embedding-3-small"
            }),
            NullLogger<OpenAIEmbeddingProviderClient>.Instance);
    }

    private sealed class SingleClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler, IDisposable
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                response.Dispose();

            base.Dispose(disposing);
        }
    }
}
