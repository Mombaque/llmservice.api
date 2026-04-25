using System.Diagnostics;
using System.Text.Json;
using LlmService.Api.Configuration;
using LlmService.Api.Contracts;
using Microsoft.Extensions.Options;

namespace LlmService.Api.Providers.OpenAI;

public class OpenAILlmProviderClient(
    OpenAIClient openAIClient,
    IOptions<OpenAIOptions> options,
    ILogger<OpenAILlmProviderClient> logger) : ILlmProviderClient
{
    public string ProviderName => "OpenAI";

    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        string requestId,
        CancellationToken cancellationToken)
    {
        var model = request.Model ?? options.Value.DefaultModel;
        if (string.IsNullOrWhiteSpace(model))
            throw new LlmProviderException("llm_model_not_configured", "OpenAI model is not configured.", StatusCodes.Status500InternalServerError);

        var openAIRequest = MapToOpenAIRequest(request, model);
        var stopwatch = Stopwatch.StartNew();
        var openAIResponse = await openAIClient.CompleteAsync(openAIRequest, requestId, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "LLM completion normalized RequestId={RequestId} Provider={Provider} Model={Model} DurationMs={DurationMs} InputTokens={InputTokens} OutputTokens={OutputTokens}",
            requestId,
            ProviderName,
            model,
            stopwatch.ElapsedMilliseconds,
            openAIResponse.Usage.PromptTokens,
            openAIResponse.Usage.CompletionTokens);

        return MapToChatCompletionResponse(openAIResponse, model, requestId);
    }

    private static OpenAIRequest MapToOpenAIRequest(ChatCompletionRequest request, string model)
    {
        var messages = new List<OpenAIMessage>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new OpenAIMessage { Role = "system", Content = request.SystemPrompt });

        messages.AddRange(request.Messages.Select(MapMessage));

        return new OpenAIRequest
        {
            Model = model,
            MaxTokens = request.MaxTokens,
            Messages = messages,
            Tools = request.Tools?.Select(MapTool).ToList()
        };
    }

    private static OpenAIMessage MapMessage(LlmMessageDto message)
    {
        if (message.Role == "tool")
        {
            var block = message.Content.First(c => c.Type == "tool_result");
            return new OpenAIMessage
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
            .Select(c => new OpenAIToolCall
            {
                Id = c.ToolCallId ?? string.Empty,
                Type = "function",
                Function = new OpenAIToolCallFunction
                {
                    Name = c.ToolName ?? string.Empty,
                    Arguments = c.ToolInput?.RootElement.GetRawText() ?? "{}"
                }
            })
            .ToList();

        return new OpenAIMessage
        {
            Role = message.Role,
            Content = string.IsNullOrEmpty(textContent) ? null : textContent,
            ToolCalls = toolCalls.Count > 0 ? toolCalls : null
        };
    }

    private static OpenAITool MapTool(LlmToolDto tool) => new()
    {
        Type = "function",
        Function = new OpenAIFunction
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = tool.InputSchema.RootElement
        }
    };

    private static ChatCompletionResponse MapToChatCompletionResponse(OpenAIResponse response, string model, string requestId)
    {
        var choice = response.Choices.FirstOrDefault();
        if (choice is null)
        {
            return new ChatCompletionResponse
            {
                StopReason = "end_turn",
                Content = [],
                Provider = "OpenAI",
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
            Provider = "OpenAI",
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
