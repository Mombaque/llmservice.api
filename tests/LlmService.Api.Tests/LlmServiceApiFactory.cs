using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LlmService.Api.Contracts;
using LlmService.Api.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LlmService.Api.Tests;

public class LlmServiceApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "dev-test-jwt-key-with-32-characters!";
    public const string JwtIssuer = "llmservice-tests";
    public const string JwtAudience = "llmservice-internal";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["OpenAI:ApiKey"] = "test-openai-key",
                ["OpenAI:DefaultModel"] = "gpt-test"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddScoped<ILlmProviderClient, FakeLlmProviderClient>();
        });
    }

    public static string CreateToken(string service, string? issuer = null, string? audience = null, string? key = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? JwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("service", service)]),
            Issuer = issuer ?? JwtIssuer,
            Audience = audience ?? JwtAudience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = credentials
        });

        return tokenHandler.WriteToken(token);
    }

    private sealed class FakeLlmProviderClient : ILlmProviderClient
    {
        public string ProviderName => "FakeOpenAI";

        public Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, string requestId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChatCompletionResponse
            {
                Provider = "FakeOpenAI",
                Model = request.Model ?? "gpt-test",
                StopReason = "stop",
                RequestId = requestId,
                Usage = new LlmUsageDto
                {
                    InputTokens = 1,
                    OutputTokens = 1
                },
                Content =
                [
                    new LlmContentBlockDto
                    {
                        Type = "text",
                        Text = "ok"
                    }
                ]
            });
        }
    }
}
