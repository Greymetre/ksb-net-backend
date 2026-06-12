using System.Text.Json;

namespace Application.Common;

public sealed record ExportHyperlink(string Text, string Url);

public static class ExportHyperlinkFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ExportHyperlink? Attachment(string? attachment, string baseUrl)
    {
        var path = FirstAttachmentPath(attachment);
        return path is null ? null : new ExportHyperlink("View", AbsoluteUrl(path, baseUrl));
    }

    private static string? FirstAttachmentPath(string? attachment)
    {
        if (string.IsNullOrWhiteSpace(attachment)) return null;
        var text = attachment.Trim();
        if (!text.StartsWith("[", StringComparison.Ordinal)) return text;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(text, JsonOptions)?
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?
                .Trim();
        }
        catch
        {
            return text;
        }
    }

    private static string AbsoluteUrl(string path, string baseUrl)
    {
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return path;
        var cleanBase = baseUrl.TrimEnd('/');
        var cleanPath = path.StartsWith('/') ? path : $"/{path}";
        return $"{cleanBase}{cleanPath}";
    }
}
