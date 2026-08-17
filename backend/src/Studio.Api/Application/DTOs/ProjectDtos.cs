using System.ComponentModel.DataAnnotations;
using Studio.Api.Domain.Enums;

namespace Studio.Api.Application.DTOs;

public record CreateProjectRequest(
    [Required, MinLength(1)] string Title,
    [Required, MinLength(10)] string BookText,
    [Required] string UserId
);

public record ProjectSummaryDto(
    string Id,
    string Title,
    ProjectStatus Status,
    StepState StepState,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CharacterDto(
    string Id,
    string Name,
    string Prompt,
    string? PortraitUrl,
    bool PortraitReady,
    int SortOrder
);

public record ChapterDto(
    string Id,
    string Name,
    string Prompt,
    List<string> Characters,
    string? IllustrationUrl,
    bool IllustrationReady,
    int SortOrder
);

public record ProjectDetailDto(
    string Id,
    string UserId,
    string Title,
    string BookText,
    ProjectStatus Status,
    StepState StepState,
    string? LastError,
    DateTime? StepStartedAt,
    string? Style,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CharacterDto> Characters,
    List<ChapterDto> Chapters
);
