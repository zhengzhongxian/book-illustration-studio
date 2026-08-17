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

public class StuckStepRecoveryTests
{
    private StudioDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudioDbContext(options);
    }

    [Fact]
    public async Task ResetStuckStep_Clears_Running_State_And_Preserves_Completed_Data()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var geminiMock = new Mock<IGeminiClient>();
        var storageMock = new Mock<ILocalStorageService>();
        var lockService = new ProjectLockService();
        var pipeline = new PipelineService(db, geminiMock.Object, storageMock.Object, lockService, NullLogger<PipelineService>.Instance);

        // Project was left in RUNNING state because of a server crash / killed process
        var project = new Project
        {
            Title = "Stuck Project",
            BookText = "Book text content...",
            Status = ProjectStatus.CHARACTERS_GENERATED,
            StepState = StepState.RUNNING,
            StepStartedAt = DateTime.UtcNow.AddMinutes(-5),
            Style = "Storybook ink style",
            Characters = new List<Character>
            {
                new() { Name = "Character 1", Prompt = "Prompt 1", PortraitReady = false },
                new() { Name = "Character 2", Prompt = "Prompt 2", PortraitReady = false }
            }
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act: User hits Reset & Retry
        var recovered = await pipeline.ResetStuckStepAsync(project.Id);

        // Assert: StepState is IDLE, existing characters and status are preserved
        Assert.Equal(StepState.IDLE, recovered.StepState);
        Assert.Null(recovered.StepStartedAt);
        Assert.Equal(ProjectStatus.CHARACTERS_GENERATED, recovered.Status);
        Assert.Equal(2, recovered.Characters.Count);
        Assert.Equal("Storybook ink style", recovered.Style);
    }
}
