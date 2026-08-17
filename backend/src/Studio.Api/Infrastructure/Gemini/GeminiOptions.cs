namespace Studio.Api.Infrastructure.Gemini;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string TextModel { get; set; } = "gemini-2.5-flash";
    public string ImageModel { get; set; } = "gemini-2.5-flash-image";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    public int TimeoutSeconds { get; set; } = 120;
}
