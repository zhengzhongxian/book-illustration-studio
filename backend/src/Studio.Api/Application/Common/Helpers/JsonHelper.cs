using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Studio.Api.Application.Common.Helpers;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static readonly JsonSerializerOptions IndentedOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value, bool indented = false)
    {
        return JsonSerializer.Serialize(value, indented ? IndentedOptions : CamelCaseOptions);
    }

    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        var cleaned = CleanJsonMarkdown(json);
        return JsonSerializer.Deserialize<T>(cleaned, CamelCaseOptions);
    }

    public static bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = Deserialize<T>(json);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Strips markdown code fences (```json ... ```) that LLMs sometimes include around JSON outputs.
    /// </summary>
    public static string CleanJsonMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var trimmed = text.Trim();

        // Regex to strip ```json ... ``` or ``` ... ```
        var match = Regex.Match(trimmed, @"^```(?:json)?\s*([\s\S]*?)\s*```$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return trimmed;
    }
}
