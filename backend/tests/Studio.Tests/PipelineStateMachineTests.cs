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

public class PipelineStateMachineTests
{
    private StudioDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudioDbContext(options);
    }

    [Fact]
    public async Task StepOrdering_Step2_Requires_Step1_Completed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var geminiMock = new Mock<IGeminiClient>();
        var storageMock = new Mock<ILocalStorageService>();
        var lockService = new ProjectLockService();
        var pipeline = new PipelineService(db, geminiMock.Object, storageMock.Object, lockService, NullLogger<PipelineService>.Instance);

        var project = new Project
        {
            Title = "Test Book",
            BookText = "Once upon a time in a faraway forest...",
            Status = ProjectStatus.CREATED,
            StepState = StepState.IDLE
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act & Assert: Attempting to run Step 2 before Step 1 must throw InvalidOperationException
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.ExecuteStepAsync(project.Id, StepKey.CHARACTERS, null));

        Assert.Contains("Step 1 (Style) must be completed", ex.Message);
    }

    [Fact]
    public async Task StepOrdering_Step3_Requires_Step2_Completed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var geminiMock = new Mock<IGeminiClient>();
        var storageMock = new Mock<ILocalStorageService>();
        var lockService = new ProjectLockService();
        var pipeline = new PipelineService(db, geminiMock.Object, storageMock.Object, lockService, NullLogger<PipelineService>.Instance);

        var project = new Project
        {
            Title = "Test Book",
            BookText = "Once upon a time...",
            Status = ProjectStatus.STYLE_SET,
            Style = "Watercolour style",
            StepState = StepState.IDLE
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.ExecuteStepAsync(project.Id, StepKey.PORTRAITS, null));

        Assert.Contains("Step 2 (Characters) must be completed", ex.Message);
    }

    [Fact]
    public async Task FullPipeline_HappyPath_Completes_All_5_Steps()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var geminiMock = new Mock<IGeminiClient>();
        var storageMock = new Mock<ILocalStorageService>();
        var lockService = new ProjectLockService();

        // Setup Gemini mocks
        geminiMock.Setup(g => g.GenerateStyleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Gentle storybook watercolour with ink contours.");

        geminiMock.Setup(g => g.ExtractCharactersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtractedCharacter>
            {
                new("Mole", "A polite and curious mole wearing a velvet coat."),
                new("Ratty", "A loyal river rat wearing a sailor shirt.")
            });

        geminiMock.Setup(g => g.GeneratePortraitImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("BASE64_IMAGE_DATA", "image/png"));

        storageMock.Setup(s => s.SaveImageBase64Async(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string folder, string id, string data, string mime) => $"{folder}/{id}.png");

        geminiMock.Setup(g => g.ExtractChaptersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ExtractedCharacter>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtractedChapter>
            {
                new("The River Bank", "Mole and Ratty meeting by the river on a sunny spring morning.", new List<string> { "Mole", "Ratty" })
            });

        geminiMock.Setup(g => g.GenerateChapterIllustrationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string, string)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("BASE64_CHAPTER_IMAGE", "image/png"));

        var pipeline = new PipelineService(db, geminiMock.Object, storageMock.Object, lockService, NullLogger<PipelineService>.Instance);

        var project = new Project
        {
            Title = "The Wind in the Willows",
            BookText = "The Mole had been working very hard all the morning, spring-cleaning his little home.",
            Status = ProjectStatus.CREATED,
            StepState = StepState.IDLE
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Step 1: STYLE
        var res1 = await pipeline.ExecuteStepAsync(project.Id, StepKey.STYLE, null);
        Assert.Equal(ProjectStatus.STYLE_SET, res1.Status);
        Assert.Equal(StepState.IDLE, res1.StepState);
        Assert.NotNull(res1.Style);

        // Step 2: CHARACTERS
        var res2 = await pipeline.ExecuteStepAsync(project.Id, StepKey.CHARACTERS, null);
        Assert.Equal(ProjectStatus.CHARACTERS_GENERATED, res2.Status);
        Assert.Equal(2, res2.Characters.Count);

        // Step 3: PORTRAITS
        var res3 = await pipeline.ExecuteStepAsync(project.Id, StepKey.PORTRAITS, null);
        Assert.Equal(ProjectStatus.PORTRAITS_GENERATED, res3.Status);
        Assert.All(res3.Characters, c => Assert.True(c.PortraitReady));

        // Step 4: CHAPTERS
        var res4 = await pipeline.ExecuteStepAsync(project.Id, StepKey.CHAPTERS, null);
        Assert.Equal(ProjectStatus.CHAPTERS_GENERATED, res4.Status);
        Assert.Single(res4.Chapters);

        // Step 5: ILLUSTRATIONS
        var res5 = await pipeline.ExecuteStepAsync(project.Id, StepKey.ILLUSTRATIONS, null);
        Assert.Equal(ProjectStatus.DONE, res5.Status);
        Assert.True(res5.Chapters[0].IllustrationReady);
        Assert.Equal(StepState.IDLE, res5.StepState);
    }
}
