namespace Studio.Api.Domain.Entities;

public class Chapter
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string CharactersJson { get; set; } = "[]";

    public string? IllustrationPath { get; set; }
    public bool IllustrationReady { get; set; } = false;

    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
