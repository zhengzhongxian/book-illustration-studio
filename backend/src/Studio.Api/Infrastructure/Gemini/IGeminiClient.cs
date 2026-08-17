namespace Studio.Api.Infrastructure.Gemini;

public interface IGeminiClient
{
    Task<string> GenerateStyleAsync(string bookText, CancellationToken ct = default);
    Task<List<ExtractedCharacter>> ExtractCharactersAsync(string bookText, string style, CancellationToken ct = default);
    Task<(string Base64Data, string MimeType)> GeneratePortraitImageAsync(string characterName, string characterPrompt, string style, CancellationToken ct = default);
    Task<List<ExtractedChapter>> ExtractChaptersAsync(string bookText, string style, List<ExtractedCharacter> characters, CancellationToken ct = default);
    Task<(string Base64Data, string MimeType)> GenerateChapterIllustrationAsync(string chapterName, string chapterPrompt, string style, List<(string CharacterName, string Base64Data)> characterReferenceImages, CancellationToken ct = default);
}
