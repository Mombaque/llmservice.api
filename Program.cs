using System.Net.Http.Headers;
using LlmService.Api.Configuration;
using LlmService.Api.Middleware;
using LlmService.Api.Providers;
using LlmService.Api.Providers.OpenAI;
using LlmService.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(Convert.ToInt32(port));
    });
}

builder.Services.AddControllers();
builder.Services.Configure<LlmGatewayOptions>(builder.Configuration.GetSection(LlmGatewayOptions.Section));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.Section));
builder.Services.Configure<ResilienceOptions>(builder.Configuration.GetSection(ResilienceOptions.Section));

builder.Services.PostConfigure<LlmGatewayOptions>(options =>
{
    options.InternalApiKey = Environment.GetEnvironmentVariable("InternalApiKey") ?? options.InternalApiKey;
});

builder.Services.AddHttpClient<OpenAIClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.ApiKey))
        throw new InvalidOperationException("OpenAI API key is not configured.");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<ILlmProviderClient, OpenAILlmProviderClient>();
builder.Services.AddScoped<LlmProviderFactory>();
builder.Services.AddScoped<LlmCompletionService>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<InternalApiKeyMiddleware>();
app.MapControllers();
app.Run();
