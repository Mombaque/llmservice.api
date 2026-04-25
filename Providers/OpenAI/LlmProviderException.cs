namespace LlmService.Api.Providers.OpenAI;

public class LlmProviderException : Exception
{
    public LlmProviderException(string code, string message, int statusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}
