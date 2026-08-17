using Studio.Api.Domain.Enums;

namespace Studio.Api.Domain.Entities;

public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string BookText { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; } = ProjectStatus.CREATED;
    public StepState StepState { get; set; } = StepState.IDLE;
    public string? LastError { get; set; }
    public DateTime? StepStartedAt { get; set; }

    public string? Style { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Character> Characters { get; set; } = new();
    public List<Chapter> Chapters { get; set; } = new();
}
