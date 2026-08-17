using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studio.Api.Infrastructure.Data;
using Studio.Api.Infrastructure.Storage;

namespace Studio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly StudioDbContext _db;
    private readonly ILocalStorageService _storage;

    public ImagesController(StudioDbContext db, ILocalStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpGet("portraits/{characterId}")]
    public async Task<IActionResult> GetCharacterPortrait(string characterId, CancellationToken ct)
    {
        var character = await _db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == characterId, ct);
        if (character == null || string.IsNullOrWhiteSpace(character.PortraitPath))
        {
            return NotFound(new { error = "Portrait not found." });
        }

        var result = _storage.GetImageStream(character.PortraitPath);
        if (result == null || result.Value.Stream == null)
        {
            return NotFound(new { error = "Portrait file not found on disk." });
        }

        return File(result.Value.Stream, result.Value.ContentType);
    }

    [HttpGet("illustrations/{chapterId}")]
    public async Task<IActionResult> GetChapterIllustration(string chapterId, CancellationToken ct)
    {
        var chapter = await _db.Chapters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chapterId, ct);
        if (chapter == null || string.IsNullOrWhiteSpace(chapter.IllustrationPath))
        {
            return NotFound(new { error = "Illustration not found." });
        }

        var result = _storage.GetImageStream(chapter.IllustrationPath);
        if (result == null || result.Value.Stream == null)
        {
            return NotFound(new { error = "Illustration file not found on disk." });
        }

        return File(result.Value.Stream, result.Value.ContentType);
    }
}
