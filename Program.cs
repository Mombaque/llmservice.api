using System.Net.Http.Headers;
using LlmService.Api.Configuration;
using LlmService.Api.Middleware;
using LlmService.Api.Providers;
using LlmService.Api.Providers.OpenAI;
using LlmService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Section))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key), "Jwt:Key is required.")
    .Validate(options => options.Key.Length >= 32, "Jwt:Key must be at least 32 characters.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
    .ValidateOnStart();

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.Section));
builder.Services.Configure<ResilienceOptions>(builder.Configuration.GetSection(ResilienceOptions.Section));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalService", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("service", "morita-api", "promotora-api", "clinic-api");
    });

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireClaim("service", "morita-api", "promotora-api", "clinic-api")
        .Build();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
