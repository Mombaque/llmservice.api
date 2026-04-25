using System.Security.Cryptography;
using System.Text;
using LlmService.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Middleware;

public class InternalApiKeyMiddleware(
    RequestDelegate next,
    IOptions<LlmGatewayOptions> options,
    ILogger<InternalApiKeyMiddleware> logger)
{
    private const string HeaderName = "X-Internal-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var configuredKey = options.Value.InternalApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            logger.LogError("Internal API key is not configured.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Internal API key is not configured." });
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var providedKey = providedValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey) || !KeysMatch(configuredKey, providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private static bool KeysMatch(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
