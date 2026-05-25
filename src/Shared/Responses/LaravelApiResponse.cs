using System.Text.Json.Serialization;

namespace Shared.Responses;

public sealed class LaravelApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "success";

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Message { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?> Extra { get; init; } = new Dictionary<string, object?>();

    public static LaravelApiResponse Success(string key, object? value, string? message = null)
    {
        var response = new LaravelApiResponse { Status = "success", Message = message };
        response.Extra[key] = value;
        return response;
    }

    public static LaravelApiResponse MessageOnly(string status, object? message) =>
        new() { Status = status, Message = message };
}
