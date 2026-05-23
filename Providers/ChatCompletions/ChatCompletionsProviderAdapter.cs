using System.Diagnostics;
using System.Text.Json;
using LlmService.Api.Configuration;
using LlmService.Api.Contracts;
using LlmService.Api.Providers;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Providers.ChatCompletions;

public abstract class ChatCompletionsProviderAdapter<TOptions>(
    IHttpClientFactory httpClientFactory,
    ChatCompletionsProviderClient client,
    IOptions<TOptions> options,
    ILogger logger) : ILlmProviderClient
    where TOptions : class, IChatCompletionsProviderOptions
{
    public abstract string ProviderName { get; }

    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        string requestId,
        CancellationToken cancellationToken)
    {
        var model = request.Model ?? options.Value.DefaultModel;
        if (string.IsNullOrWhiteSpace(model))
            throw new LlmProviderException("llm_model_not_configured", $"{ProviderName} model is not configured.", StatusCodes.Status500InternalServerError);

        var providerRequest = MapToProviderRequest(request, model);
        var stopwatch = Stopwatch.StartNew();
        var httpClient = httpClientFactory.CreateClient(ProviderName);
        var providerResponse = await client.CompleteAsync(httpClient, providerRequest, requestId, ProviderName, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "LLM completion normalized RequestId={RequestId} Provider={Provider} Model={Model} DurationMs={DurationMs} InputTokens={InputTokens} OutputTokens={OutputTokens}",
            requestId,
            ProviderName,
            model,
            stopwatch.ElapsedMilliseconds,
            providerResponse.Usage.PromptTokens,
            providerResponse.Usage.CompletionTokens);

        return MapToChatCompletionResponse(providerResponse, model, requestId);
    }

    private static ChatCompletionsProviderRequest MapToProviderRequest(ChatCompletionRequest request, string model)
    {
        var messages = new List<ChatCompletionsProviderMessage>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new ChatCompletionsProviderMessage { Role = "system", Content = request.SystemPrompt });

        messages.AddRange(request.Messages.Select(MapMessage));

        return new ChatCompletionsProviderRequest
        {
            Model = model,
            MaxTokens = request.MaxTokens,
            Messages = messages,
            Tools = request.Tools?.Select(MapTool).ToList()
        };
    }

    private static ChatCompletionsProviderMessage MapMessage(LlmMessageDto message)
    {
        if (message.Role == "tool")
        {
            var block = message.Content.First(c => c.Type == "tool_result");
            return new ChatCompletionsProviderMessage
            {
                Role = "tool",
                ToolCallId = block.ToolCallId,
                Content = block.Text
            };
        }

        var textContent = string.Join("\n", message.Content
            .Where(c => c.Type == "text")
            .Select(c => c.Text ?? string.Empty));

        var toolCalls = message.Content
            .Where(c => c.Type == "tool_call")
            .Select(c => new ChatCompletionsProviderToolCall
            {
                Id = c.ToolCallId ?? string.Empty,
                Type = "function",
                Function = new ChatCompletionsProviderToolCallFunction
                {
                    Name = c.ToolName ?? string.Empty,
                    Arguments = c.ToolInput?.RootElement.GetRawText() ?? "{}"
                }
            })
            .ToList();

        return new ChatCompletionsProviderMessage
        {
            Role = message.Role,
            Content = string.IsNullOrEmpty(textContent) ? null : textContent,
            ToolCalls = toolCalls.Count > 0 ? toolCalls : null
        };
    }

    private static ChatCompletionsProviderTool MapTool(LlmToolDto tool) => new()
    {
        Type = "function",
        Function = new ChatCompletionsProviderFunction
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = tool.InputSchema.RootElement
        }
    };

    private ChatCompletionResponse MapToChatCompletionResponse(ChatCompletionsProviderResponse response, string model, string requestId)
    {
        var choice = response.Choices.FirstOrDefault();
        if (choice is null)
        {
            return new ChatCompletionResponse
            {
                StopReason = "end_turn",
                Content = [],
                Provider = ProviderName,
                Model = model,
                RequestId = requestId,
                Usage = new LlmUsageDto()
            };
        }

        var blocks = new List<LlmContentBlockDto>();
        var message = choice.Message;

        if (!string.IsNullOrEmpty(message.Content))
        {
            blocks.Add(new LlmContentBlockDto
            {
                Type = "text",
                Text = message.Content
            });
        }

        if (message.ToolCalls is not null)
        {
            foreach (var toolCall in message.ToolCalls)
            {
                blocks.Add(new LlmContentBlockDto
                {
                    Type = "tool_call",
                    ToolCallId = toolCall.Id,
                    ToolName = toolCall.Function.Name,
                    ToolInput = JsonDocument.Parse(toolCall.Function.Arguments)
                });
            }
        }

        return new ChatCompletionResponse
        {
            StopReason = choice.FinishReason == "tool_calls" ? "tool_call" : "end_turn",
            Content = blocks.ToArray(),
            Provider = ProviderName,
            Model = model,
            RequestId = requestId,
            Usage = new LlmUsageDto
            {
                InputTokens = response.Usage.PromptTokens,
                OutputTokens = response.Usage.CompletionTokens
            }
        };
    }
}
