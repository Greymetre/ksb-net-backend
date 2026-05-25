namespace Shared.Exceptions;

public sealed class LaravelHttpException : Exception
{
    public LaravelHttpException(int statusCode, object message) : base(message?.ToString())
    {
        StatusCode = statusCode;
        ResponseMessage = message ?? string.Empty;
    }

    public int StatusCode { get; }
    public object ResponseMessage { get; }
}
