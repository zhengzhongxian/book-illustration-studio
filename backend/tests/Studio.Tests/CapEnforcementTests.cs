using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Studio.Api.Application.Services;
using Studio.Api.Domain.Entities;
using Studio.Api.Domain.Enums;
using Studio.Api.Infrastructure.Concurrency;
using Studio.Api.Infrastructure.Data;
using Studio.Api.Infrastructure.Gemini;
using Studio.Api.Infrastructure.Storage;

namespace Studio.Tests;

public class CapEnforcementTests
{
    private StudioDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudioDbContext(options);
    }

    [Fact]
    public async Task CharacterCap_Enforces_Max_2_Adult_Characters_ServerSide()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var geminiMock = new Mock<IGeminiClient>();
        var storageMock = new Mock<ILocalStorageService>();
        var lockService = new ProjectLockService();

        // LLM hypothetically returns 5 characters
        geminiMock.Setup(g => g.ExtractCharactersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtractedCharacter>
            {
                new("Mole", "Mole prompt"),
                new("Ratty", "Ratty prompt"),
                new("Toad", "Toad prompt"),
                new("Badger", "Badger prompt"),
                new("Otter", "Otter prompt")
            });

        var pipeline = new PipelineService(db, geminiMock.Object, storageMock.Object, lockService, NullLogger<PipelineService>.Instance);

        var project = new Project
        {
            Title = "Test Book",
            BookText = "Book text...",
            Status = ProjectStatus.STYLE_SET,
            Style = "Watercolour style",
            StepState = StepState.IDLE
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act
        var result = await pipeline.ExecuteStepAsync(project.Id, StepKey.CHARACTERS, null);

        // Assert: Must be capped at exactly 2
        Assert.Equal(2, result.Characters.Count);
        var dbProject = await db.Projects.Include(p => p.Characters).FirstAsync(p => p.Id == project.Id);
        Assert.Equal(2, dbProject.Characters.Count);
    }

    [Fact]
    public async Task ChapterCap_Enforces_Max_1_Chapter_Scene_ServerSide()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var geminiMock = new Mock<IGeminiClient>();
        var storageMock = new Mock<ILocalStorageService>();
        var lockService = new ProjectLockService();

        // LLM hypothetically returns 3 chapters
        geminiMock.Setup(g => g.ExtractChaptersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ExtractedCharacter>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtractedChapter>
            {
                new("Chapter 1: The River Bank", "Scene 1 description", new List<string> { "Mole" }),
                new("Chapter 2: The Open Road", "Scene 2 description", new List<string> { "Toad" }),
                new("Chapter 3: The Wild Wood", "Scene 3 description", new List<string> { "Badger" })
            });

        var pipeline = new PipelineService(db, geminiMock.Object, storageMock.Object, lockService, NullLogger<PipelineService>.Instance);

        var project = new Project
        {
            Title = "Test Book",
            BookText = "Book text...",
            Status = ProjectStatus.PORTRAITS_GENERATED,
            Style = "Watercolour style",
            StepState = StepState.IDLE,
            Characters = new List<Character>
            {
                new() { Name = "Mole", Prompt = "Prompt", PortraitReady = true }
            }
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act
        var result = await pipeline.ExecuteStepAsync(project.Id, StepKey.CHAPTERS, null);

        // Assert: Must be capped at exactly 1
        Assert.Single(result.Chapters);
        var dbProject = await db.Projects.Include(p => p.Chapters).FirstAsync(p => p.Id == project.Id);
        Assert.Single(dbProject.Chapters);
    }
}
