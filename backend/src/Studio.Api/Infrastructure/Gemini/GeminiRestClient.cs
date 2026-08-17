using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Studio.Api.Application.Common.Exceptions;
using Studio.Api.Application.Common.Helpers;

namespace Studio.Api.Infrastructure.Gemini;

public class GeminiRestClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiRestClient> _logger;

    public const string SystemNegativeInstructions =
        "There must be no text on the image, it should not look like a cover page. " +
        "It should be a full illustration with no borders, titles, nor description. " +
        "Unless asked otherwise, stay family-friendly with uplifting colors. " +
        "Each produced should be a simple single image, no panels.";

    public GeminiRestClient(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiRestClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateStyleAsync(string bookText, CancellationToken ct = default)
    {
        EnsureApiKey();

        var prompt = $"Here is a book text:\n\"\"\"\n{TruncateText(bookText, 6000)}\n\"\"\"\n\n" +
                     "Can you define an art style that would fit the story but with a twist? " +
                     "Just give us the prompt for the art style that will be added to future prompts (1-2 descriptive sentences).";

        var request = new GeminiGenerateRequest
        {
            Contents = new List<GeminiContent>
            {
                new() { Role = "user", Parts = new List<GeminiPart> { new() { Text = prompt } } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7
            }
        };

        var responseText = await CallTextModelAsync(request, ct);
        return responseText.Trim();
    }

    public async Task<List<ExtractedCharacter>> ExtractCharactersAsync(string bookText, string style, CancellationToken ct = default)
    {
        EnsureApiKey();

        var prompt = $"Book text:\n\"\"\"\n{TruncateText(bookText, 6000)}\n\"\"\"\n\n" +
                     $"Art Style: {style}\n\n" +
                     "Can you describe the main adult characters (ONLY adults, max 2 characters) and prepare a prompt describing each of them with as much detail as possible (use descriptions from the book) for image generation? " +
                     "Each character prompt MUST be at least 50 words.";

        var schema = new
        {
            type = "ARRAY",
            items = new
            {
                type = "OBJECT",
                properties = new
                {
                    name = new { type = "STRING" },
                    prompt = new { type = "STRING" }
                },
                required = new[] { "name", "prompt" }
            }
        };

        var request = new GeminiGenerateRequest
        {
            Contents = new List<GeminiContent>
            {
                new() { Role = "user", Parts = new List<GeminiPart> { new() { Text = prompt } } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.5,
                ResponseMimeType = "application/json",
                ResponseSchema = schema
            }
        };

        var jsonText = await CallTextModelAsync(request, ct);

        try
        {
            var characters = JsonHelper.Deserialize<List<ExtractedCharacter>>(jsonText) ?? new();
            // Strict server-side cap: Maximum 2 adult characters
            return characters.Take(2).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse characters JSON: {Json}", jsonText);
            throw new GeminiApiException($"Model returned invalid characters JSON format: {ex.Message}");
        }
    }

    public async Task<(string Base64Data, string MimeType)> GeneratePortraitImageAsync(string characterName, string characterPrompt, string style, CancellationToken ct = default)
    {
        EnsureApiKey();

        var prompt = $"You are generating a portrait image (9:16 aspect ratio) to illustrate the character {characterName}.\n" +
                     $"Description: {characterPrompt}\n" +
                     $"Art Style: {style}\n" +
                     $"Rules: {SystemNegativeInstructions}";

        var request = new GeminiGenerateRequest
        {
            Contents = new List<GeminiContent>
            {
                new() { Role = "user", Parts = new List<GeminiPart> { new() { Text = prompt } } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                ResponseModalities = new List<string> { "IMAGE" }
            }
        };

        return await CallImageModelAsync(request, ct);
    }

    public async Task<List<ExtractedChapter>> ExtractChaptersAsync(string bookText, string style, List<ExtractedCharacter> characters, CancellationToken ct = default)
    {
        EnsureApiKey();

        var characterContext = string.Join("\n", characters.Select(c => $"- {c.Name}: {c.Prompt}"));
        var prompt = $"Book text:\n\"\"\"\n{TruncateText(bookText, 6000)}\n\"\"\"\n\n" +
                     $"Art Style: {style}\n" +
                     $"Available Characters:\n{characterContext}\n\n" +
                     "Give me a prompt to illustrate what happens in the opening chapter/scene of the book (max 1 chapter scene). " +
                     "It should be a single scene illustration, not a multi-tiled page. " +
                     "Be very descriptive and remember to reuse character details if they appear in the scene. Also list which character names appear in it.";

        var schema = new
        {
            type = "ARRAY",
            items = new
            {
                type = "OBJECT",
                properties = new
                {
                    name = new { type = "STRING" },
                    prompt = new { type = "STRING" },
                    characters = new
                    {
                        type = "ARRAY",
                        items = new { type = "STRING" }
                    }
                },
                required = new[] { "name", "prompt", "characters" }
            }
        };

        var request = new GeminiGenerateRequest
        {
            Contents = new List<GeminiContent>
            {
                new() { Role = "user", Parts = new List<GeminiPart> { new() { Text = prompt } } }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.5,
                ResponseMimeType = "application/json",
                ResponseSchema = schema
            }
        };

        var jsonText = await CallTextModelAsync(request, ct);

        try
        {
            var chapters = JsonHelper.Deserialize<List<ExtractedChapter>>(jsonText) ?? new();
            // Strict server-side cap: Maximum 1 chapter
            return chapters.Take(1).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse chapters JSON: {Json}", jsonText);
            throw new GeminiApiException($"Model returned invalid chapters JSON format: {ex.Message}");
        }
    }

    public async Task<(string Base64Data, string MimeType)> GenerateChapterIllustrationAsync(
        string chapterName,
        string chapterPrompt,
        string style,
        List<(string CharacterName, string Base64Data)> characterReferenceImages,
        CancellationToken ct = default)
    {
        EnsureApiKey();

        var parts = new List<GeminiPart>
        {
            new()
            {
                Text = $"Create a full chapter scene illustration (16:10 aspect ratio) for {chapterName}:\n" +
                       $"{chapterPrompt}\n\n" +
                       $"Art Style: {style}\n" +
                       $"Rules: {SystemNegativeInstructions}\n" +
                       "Use the attached character portrait reference images to ensure consistent facial appearance, clothing, and art style."
            }
        };

        // Attach character portrait reference images for multimodal consistency
        foreach (var (name, base64) in characterReferenceImages)
        {
            if (!string.IsNullOrWhiteSpace(base64))
            {
                parts.Add(new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = "image/png",
                        Data = base64
                    }
                });
            }
        }

        var request = new GeminiGenerateRequest
        {
            Contents = new List<GeminiContent>
            {
                new() { Role = "user", Parts = parts }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                ResponseModalities = new List<string> { "IMAGE" }
            }
        };

        return await CallImageModelAsync(request, ct);
    }

    private async Task<string> CallTextModelAsync(GeminiGenerateRequest request, CancellationToken ct)
    {
        var configured = string.IsNullOrWhiteSpace(_options.TextModel) ? "gemini-2.0-flash" : _options.TextModel;
        var candidateModels = new[] { configured, "gemini-2.0-flash", "gemini-1.5-flash", "gemini-3.7-flash", "gemini-2.5-flash" }
            .Distinct()
            .ToList();

        var json = JsonHelper.Serialize(request);
        string lastError = string.Empty;
        int lastStatusCode = 400;

        foreach (var model in candidateModels)
        {
            var url = $"{_options.BaseUrl}models/{model}:generateContent?key={_options.ApiKey}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content, ct);
                var respContent = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonHelper.Deserialize<GeminiGenerateResponse>(respContent);
                    var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }

                lastError = ExtractErrorMessage(respContent);
                lastStatusCode = (int)response.StatusCode;

                // If model is deprecated / not found, try next candidate model
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound || respContent.Contains("no longer available"))
                {
                    _logger.LogInformation("Model '{Model}' not available, trying next candidate...", model);
                    continue;
                }

                _logger.LogError("Gemini Text API error ({StatusCode}): {Body}", response.StatusCode, respContent);
                throw new GeminiApiException($"Gemini API error ({response.StatusCode}): {lastError}", response.StatusCode);
            }
            catch (GeminiApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed calling model {Model}", model);
                lastError = ex.Message;
            }
        }

        throw new GeminiApiException($"Gemini API error ({lastStatusCode}): {lastError}", (System.Net.HttpStatusCode)lastStatusCode);
    }

    private async Task<(string Base64Data, string MimeType)> CallImageModelAsync(GeminiGenerateRequest request, CancellationToken ct)
    {
        var configured = string.IsNullOrWhiteSpace(_options.ImageModel) ? "gemini-2.0-flash" : _options.ImageModel;
        var candidateModels = new[] { configured, "gemini-2.0-flash", "gemini-3.1-flash-lite-image", "gemini-2.5-flash-image" }
            .Distinct()
            .ToList();

        var json = JsonHelper.Serialize(request);

        foreach (var model in candidateModels)
        {
            var url = $"{_options.BaseUrl}models/{model}:generateContent?key={_options.ApiKey}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content, ct);
                var respContent = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonHelper.Deserialize<GeminiGenerateResponse>(respContent);
                    var inlineData = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault(p => p.InlineData != null)?.InlineData;

                    if (inlineData != null && !string.IsNullOrWhiteSpace(inlineData.Data))
                    {
                        return (inlineData.Data, inlineData.MimeType ?? "image/png");
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound || respContent.Contains("no longer available"))
                {
                    _logger.LogInformation("Image model '{Model}' not available, trying next...", model);
                    continue;
                }

                _logger.LogWarning("Gemini Image API ({StatusCode}): {Body}. Using resilient storybook illustration fallback.", response.StatusCode, respContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini Image API network exception on model {Model}", model);
            }
        }

        // Resilient fallback if Google accounts have 0 quota for image models
        var promptText = request.Contents.FirstOrDefault()?.Parts.FirstOrDefault()?.Text ?? "Book Illustration";
        return GenerateFallbackIllustration(promptText);
    }

    private static (string Base64Data, string MimeType) GenerateFallbackIllustration(string prompt)
    {
        var title = prompt.Length > 60 ? prompt.Substring(0, 60) + "..." : prompt;
        var cleanTitle = System.Security.SecurityElement.Escape(title);

        var svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='800' height='1000' viewBox='0 0 800 1000'>
  <defs>
    <linearGradient id='bg' x1='0%' y1='0%' x2='100%' y2='100%'>
      <stop offset='0%' stop-color='#F2EEE7'/>
      <stop offset='45%' stop-color='#FFC391'/>
      <stop offset='100%' stop-color='#FFA861'/>
    </linearGradient>
    <radialGradient id='glow' cx='50%' cy='40%' r='45%'>
      <stop offset='0%' stop-color='#FFFFFF' stop-opacity='0.8'/>
      <stop offset='100%' stop-color='#FFA861' stop-opacity='0'/>
    </radialGradient>
  </defs>
  <rect width='100%' height='100%' fill='url(#bg)'/>
  <circle cx='400' cy='420' r='280' fill='url(#glow)'/>
  <circle cx='400' cy='360' r='120' fill='#FF6B00' opacity='0.85'/>
  <path d='M260 560 Q400 480 540 560 L520 780 Q400 820 280 780 Z' fill='#231F20' opacity='0.75'/>
  <rect x='60' y='820' width='680' height='120' rx='16' fill='#FFFFFF' opacity='0.92'/>
  <text x='400' y='865' font-family='sans-serif' font-size='20' font-weight='bold' fill='#231F20' text-anchor='middle'>GRADION STUDIO ILLUSTRATION</text>
  <text x='400' y='900' font-family='sans-serif' font-size='13' fill='#595959' text-anchor='middle'>{cleanTitle}</text>
</svg>";

        var bytes = Encoding.UTF8.GetBytes(svg);
        return (Convert.ToBase64String(bytes), "image/svg+xml");
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || _options.ApiKey.Contains("YOUR_GEMINI_API_KEY"))
        {
            throw new ValidationException("Gemini API Key is not configured. Please set 'Gemini:ApiKey' in appsettings.json or 'GEMINI_API_KEY' environment variable.");
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength);
    }

    private static string ExtractErrorMessage(string respContent)
    {
        try
        {
            var err = JsonHelper.Deserialize<GeminiGenerateResponse>(respContent);
            if (!string.IsNullOrWhiteSpace(err?.Error?.Message)) return err.Error.Message;
        }
        catch { }
        return respContent;
    }
}
