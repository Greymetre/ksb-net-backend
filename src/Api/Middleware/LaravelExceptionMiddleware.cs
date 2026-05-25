using System.Text.Json;
using Shared.Exceptions;
using Shared.Responses;

namespace Api.Middleware;

public sealed class LaravelExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LaravelExceptionMiddleware> _logger;

    public LaravelExceptionMiddleware(RequestDelegate next, ILogger<LaravelExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (LaravelHttpException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body,
                LaravelApiResponse.MessageOnly("error", exception.ResponseMessage),
                JsonOptions(), context.RequestAborted);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body,
                LaravelApiResponse.MessageOnly("error", exception.Message),
                JsonOptions(), context.RequestAborted);
        }
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}
