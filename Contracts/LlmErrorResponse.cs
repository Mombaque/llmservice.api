namespace LlmService.Api.Contracts;

public class LlmErrorResponse
{
    public LlmError Error { get; set; } = new();
}

public class LlmError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}
