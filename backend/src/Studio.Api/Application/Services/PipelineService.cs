using Microsoft.EntityFrameworkCore;
using Studio.Api.Application.DTOs;
using Studio.Api.Domain.Entities;
using Studio.Api.Domain.Enums;
using Studio.Api.Infrastructure.Concurrency;
using Studio.Api.Infrastructure.Data;
using Studio.Api.Infrastructure.Gemini;
using Studio.Api.Infrastructure.Storage;

namespace Studio.Api.Application.Services;

public interface IPipelineService
{
    Task<ProjectDetailDto> ExecuteStepAsync(string projectId, StepKey step, string? customStyle, CancellationToken ct = default);
    Task<ProjectDetailDto> ResetStuckStepAsync(string projectId, CancellationToken ct = default);
}

public class PipelineService : IPipelineService
{
    private readonly StudioDbContext _db;
    private readonly IGeminiClient _gemini;
    private readonly ILocalStorageService _storage;
    private readonly IProjectLockService _lockService;
    private readonly ILogger<PipelineService> _logger;

    public PipelineService(
        StudioDbContext db,
        IGeminiClient gemini,
        ILocalStorageService storage,
        IProjectLockService lockService,
        ILogger<PipelineService> logger)
    {
        _db = db;
        _gemini = gemini;
        _storage = storage;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task<ProjectDetailDto> ExecuteStepAsync(string projectId, StepKey step, string? customStyle, CancellationToken ct = default)
    {
        // 1. Concurrency Guard: Non-blocking in-memory lock
        if (!_lockService.TryAcquire(projectId, out var lockReleaser))
        {
            throw new InvalidOperationException("This project is currently processing a step. Duplicate execution is prevented.");
        }

        using (lockReleaser)
        {
            var project = await _db.Projects
                .Include(p => p.Characters.OrderBy(c => c.SortOrder))
                .Include(p => p.Chapters.OrderBy(c => c.SortOrder))
                .FirstOrDefaultAsync(p => p.Id == projectId, ct);

            if (project == null) throw new KeyNotFoundException("Project not found.");

            // 2. Validate prerequisites & step sequence
            ValidateStepPrerequisites(project, step);

            // 3. Mark in-progress
            _logger.LogInformation("--> [Pipeline] Starting Step '{Step}' for Project '{Title}' (Id: {ProjectId})", step, project.Title, project.Id);
            project.StepState = StepState.RUNNING;
            project.StepStartedAt = DateTime.UtcNow;
            project.LastError = null;
            await _db.SaveChangesAsync(ct);

            try
            {
                switch (step)
                {
                    case StepKey.STYLE:
                        await RunStyleStepAsync(project, customStyle, ct);
                        break;
                    case StepKey.CHARACTERS:
                        await RunCharactersStepAsync(project, ct);
                        break;
                    case StepKey.PORTRAITS:
                        await RunPortraitsStepAsync(project, ct);
                        break;
                    case StepKey.CHAPTERS:
                        await RunChaptersStepAsync(project, ct);
                        break;
                    case StepKey.ILLUSTRATIONS:
                        await RunIllustrationsStepAsync(project, ct);
                        break;
                }

                project.StepState = StepState.IDLE;
                project.StepStartedAt = null;
                project.LastError = null;
                project.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("<-- [Pipeline] Step '{Step}' completed successfully for Project '{Title}'. Milestone: {Status}", step, project.Title, project.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XXX [Pipeline] Step '{Step}' failed for Project '{Title}' ({ProjectId}): {Error}", step, project.Title, projectId, ex.Message);
                project.StepState = StepState.FAILED;
                project.LastError = ex.Message;
                project.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                throw;
            }

            return ProjectService.MapToDetailDto(project);
        }
    }


    public async Task<ProjectDetailDto> ResetStuckStepAsync(string projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects
            .Include(p => p.Characters.OrderBy(c => c.SortOrder))
            .Include(p => p.Chapters.OrderBy(c => c.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project == null) throw new KeyNotFoundException("Project not found.");

        project.StepState = StepState.IDLE;
        project.StepStartedAt = null;
        project.LastError = null;
        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ProjectService.MapToDetailDto(project);
    }

    private static void ValidateStepPrerequisites(Project project, StepKey step)
    {
        if (project.StepState == StepState.RUNNING)
        {
            throw new InvalidOperationException("A step is already running on this project.");
        }

        switch (step)
        {
            case StepKey.STYLE:
                // Step 1 can be run when CREATED or retried
                break;

            case StepKey.CHARACTERS:
                if (project.Status < ProjectStatus.STYLE_SET)
                    throw new InvalidOperationException("Step 1 (Style) must be completed before extracting characters.");
                break;

            case StepKey.PORTRAITS:
                if (project.Status < ProjectStatus.CHARACTERS_GENERATED || !project.Characters.Any())
                    throw new InvalidOperationException("Step 2 (Characters) must be completed before generating portraits.");
                break;

            case StepKey.CHAPTERS:
                if (project.Status < ProjectStatus.PORTRAITS_GENERATED)
                    throw new InvalidOperationException("Step 3 (Portraits) must be completed before extracting chapters.");
                break;

            case StepKey.ILLUSTRATIONS:
                if (project.Status < ProjectStatus.CHAPTERS_GENERATED || !project.Chapters.Any())
                    throw new InvalidOperationException("Step 4 (Chapters) must be completed before generating illustrations.");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(step), step, "Unknown step key.");
        }
    }

    private async Task RunStyleStepAsync(Project project, string? customStyle, CancellationToken ct)
    {
        string styleText;
        if (!string.IsNullOrWhiteSpace(customStyle))
        {
            styleText = $"{customStyle.Trim()} (as specified — keep this style consistent for all subsequent prompts).";
            _logger.LogInformation("[Pipeline:Style] Using user-specified custom style: {Style}", styleText);
        }
        else
        {
            _logger.LogInformation("[Pipeline:Style] Requesting Gemini to auto-generate art style from book text...");
            styleText = await _gemini.GenerateStyleAsync(project.BookText, ct);
            _logger.LogInformation("[Pipeline:Style] Generated art style: {Style}", styleText);
        }

        project.Style = styleText;
        project.Status = ProjectStatus.STYLE_SET;
    }

    private async Task RunCharactersStepAsync(Project project, CancellationToken ct)
    {
        var style = project.Style ?? "Storybook illustration";
        _logger.LogInformation("[Pipeline:Characters] Extracting adult characters with style '{Style}'...", style);
        var characters = await _gemini.ExtractCharactersAsync(project.BookText, style, ct);

        if (!characters.Any())
        {
            throw new InvalidOperationException("Failed to extract adult characters from the book text.");
        }

        // Hard Limit: Max 2 adult characters
        var cappedCharacters = characters.Take(2).ToList();

        // Clear existing characters if re-running step 2
        _db.Characters.RemoveRange(project.Characters);
        project.Characters.Clear();

        for (int i = 0; i < cappedCharacters.Count; i++)
        {
            var c = cappedCharacters[i];
            project.Characters.Add(new Character
            {
                ProjectId = project.Id,
                Name = c.Name,
                Prompt = c.Prompt,
                SortOrder = i,
                PortraitReady = false
            });
        }

        _logger.LogInformation("[Pipeline:Characters] Extracted {Count} adult characters (Cap: 2): {Names}",
            project.Characters.Count, string.Join(", ", project.Characters.Select(c => c.Name)));

        project.Status = ProjectStatus.CHARACTERS_GENERATED;
    }

    private async Task RunPortraitsStepAsync(Project project, CancellationToken ct)
    {
        var style = project.Style ?? "Storybook illustration";
        _logger.LogInformation("[Pipeline:Portraits] Generating {Count} portrait images (9:16)...", project.Characters.Count);

        foreach (var character in project.Characters.OrderBy(c => c.SortOrder))
        {
            _logger.LogInformation("[Pipeline:Portraits] Generating portrait for '{Name}'...", character.Name);
            var (base64, mimeType) = await _gemini.GeneratePortraitImageAsync(character.Name, character.Prompt, style, ct);
            var relativePath = await _storage.SaveImageBase64Async("portraits", character.Id, base64, mimeType);

            character.PortraitPath = relativePath;
            character.PortraitReady = true;

            // Save incremental progress so portraits reveal progressively
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("[Pipeline:Portraits] Portrait ready for '{Name}': {Path}", character.Name, relativePath);
        }

        project.Status = ProjectStatus.PORTRAITS_GENERATED;
    }

    private async Task RunChaptersStepAsync(Project project, CancellationToken ct)
    {
        var style = project.Style ?? "Storybook illustration";
        var charList = project.Characters.Select(c => new ExtractedCharacter(c.Name, c.Prompt)).ToList();

        _logger.LogInformation("[Pipeline:Chapters] Extracting chapter scenes referencing {CharCount} characters...", charList.Count);
        var chapters = await _gemini.ExtractChaptersAsync(project.BookText, style, charList, ct);
        if (!chapters.Any())
        {
            throw new InvalidOperationException("Failed to extract chapter prompt from the book text.");
        }

        // Hard Limit: Max 1 chapter scene
        var cappedChapters = chapters.Take(1).ToList();

        // Clear existing chapters if re-running step 4
        _db.Chapters.RemoveRange(project.Chapters);
        project.Chapters.Clear();

        for (int i = 0; i < cappedChapters.Count; i++)
        {
            var ch = cappedChapters[i];
            var charJson = System.Text.Json.JsonSerializer.Serialize(ch.Characters ?? new List<string>());

            project.Chapters.Add(new Chapter
            {
                ProjectId = project.Id,
                Name = ch.Name,
                Prompt = ch.Prompt,
                CharactersJson = charJson,
                SortOrder = i,
                IllustrationReady = false
            });
        }

        _logger.LogInformation("[Pipeline:Chapters] Extracted chapter: '{Name}' (Cap: 1)", project.Chapters.FirstOrDefault()?.Name);
        project.Status = ProjectStatus.CHAPTERS_GENERATED;
    }

    private async Task RunIllustrationsStepAsync(Project project, CancellationToken ct)
    {
        var style = project.Style ?? "Storybook illustration";

        // Load reference images of generated portraits
        var refImages = new List<(string CharacterName, string Base64Data)>();
        foreach (var character in project.Characters)
        {
            if (!string.IsNullOrEmpty(character.PortraitPath))
            {
                var b64 = await _storage.ReadImageAsBase64Async(character.PortraitPath);
                if (!string.IsNullOrEmpty(b64))
                {
                    refImages.Add((character.Name, b64));
                }
            }
        }

        _logger.LogInformation("[Pipeline:Illustrations] Generating scene illustrations (16:10) with {RefCount} character reference image(s)...", refImages.Count);

        foreach (var chapter in project.Chapters.OrderBy(c => c.SortOrder))
        {
            _logger.LogInformation("[Pipeline:Illustrations] Generating illustration for '{Chapter}'...", chapter.Name);
            var (base64, mimeType) = await _gemini.GenerateChapterIllustrationAsync(
                chapter.Name,
                chapter.Prompt,
                style,
                refImages,
                ct);

            var relativePath = await _storage.SaveImageBase64Async("illustrations", chapter.Id, base64, mimeType);

            chapter.IllustrationPath = relativePath;
            chapter.IllustrationReady = true;

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("[Pipeline:Illustrations] Illustration ready for '{Chapter}': {Path}", chapter.Name, relativePath);
        }

        project.Status = ProjectStatus.DONE;
    }

}
