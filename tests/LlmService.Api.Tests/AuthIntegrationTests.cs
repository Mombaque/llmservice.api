using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace LlmService.Api.Tests;

public class AuthIntegrationTests : IClassFixture<LlmServiceApiFactory>
{
    private const string ValidRequestBody = """
        {
          "messages": [
            {
              "role": "user",
              "content": [
                {
                  "type": "text",
                  "text": "Hello"
                }
              ]
            }
          ]
        }
        """;

    private readonly HttpClient _client;

    public AuthIntegrationTests(LlmServiceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChatCompletions_WithoutToken_Returns401()
    {
        using var request = CreateChatRequest();

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletions_WithInvalidToken_Returns401()
    {
        using var request = CreateChatRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletions_WithWrongIssuer_Returns401()
    {
        using var request = CreateChatRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            LlmServiceApiFactory.CreateToken("morita-api", issuer: "wrong-issuer"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletions_WithWrongAudience_Returns401()
    {
        using var request = CreateChatRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            LlmServiceApiFactory.CreateToken("morita-api", audience: "wrong-audience"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletions_WithUnauthorizedService_Returns403()
    {
        using var request = CreateChatRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            LlmServiceApiFactory.CreateToken("unknown-api"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChatCompletions_WithValidToken_Returns200()
    {
        using var request = CreateChatRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            LlmServiceApiFactory.CreateToken("morita-api"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_RemainsPublic()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpRequestMessage CreateChatRequest()
    {
        return new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(ValidRequestBody, Encoding.UTF8, "application/json")
        };
    }
}
