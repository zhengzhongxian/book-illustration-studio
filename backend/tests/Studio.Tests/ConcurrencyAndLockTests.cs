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

public class ConcurrencyAndLockTests
{
    private StudioDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<StudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StudioDbContext(options);
    }

    [Fact]
    public async Task ConcurrencyGuard_Prevents_Duplicate_Running_Calls()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var geminiMock = new Mock<IGeminiClient>();
        var storageMock = new Mock<ILocalStorageService>();
        var lockService = new ProjectLockService();

        var tcs = new TaskCompletionSource<string>();

        // Make Gemini hang until explicitly released
        geminiMock.Setup(g => g.GenerateStyleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var pipeline = new PipelineService(db, geminiMock.Object, storageMock.Object, lockService, NullLogger<PipelineService>.Instance);

        var project = new Project
        {
            Title = "Concurrency Test",
            BookText = "Once upon a time...",
            Status = ProjectStatus.CREATED,
            StepState = StepState.IDLE
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act: Fire first request (it will acquire lock and wait on tcs)
        var task1 = pipeline.ExecuteStepAsync(project.Id, StepKey.STYLE, null);

        // Attempt second concurrent request on same project
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.ExecuteStepAsync(project.Id, StepKey.STYLE, null));

        Assert.Contains("currently processing a step", ex.Message);

        // Release first task
        tcs.SetResult("Generated style");
        var result1 = await task1;
        Assert.Equal(ProjectStatus.STYLE_SET, result1.Status);
    }
}
