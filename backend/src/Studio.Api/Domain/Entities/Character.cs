namespace Studio.Api.Domain.Entities;

public class Character
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;

    public string? PortraitPath { get; set; }
    public bool PortraitReady { get; set; } = false;

    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
