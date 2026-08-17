using Microsoft.AspNetCore.Mvc;
using Studio.Api.Application.Common;
using Studio.Api.Application.Common.Exceptions;
using Studio.Api.Application.DTOs;
using Studio.Api.Application.Services;
using Studio.Api.Domain.Enums;

namespace Studio.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId}/[controller]")]
public class PipelineController : ControllerBase
{
    private readonly IPipelineService _pipelineService;

    public PipelineController(IPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    [HttpPost("steps/{stepKey}")]
    public async Task<IActionResult> RunStep(
        string projectId,
        string stepKey,
        [FromBody] RunStepRequest? request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<StepKey>(stepKey, true, out var parsedStep))
        {
            throw new ValidationException($"Invalid step key '{stepKey}'. Valid steps: STYLE, CHARACTERS, PORTRAITS, CHAPTERS, ILLUSTRATIONS.");
        }

        var updated = await _pipelineService.ExecuteStepAsync(projectId, parsedStep, request?.CustomStyle, ct);
        return StatusCode(StatusCodes.Status200OK, ApiResponse<ProjectDetailDto>.Ok(updated, $"Step {stepKey} completed successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("reset-stuck")]
    public async Task<IActionResult> ResetStuck(string projectId, CancellationToken ct)
    {
        var updated = await _pipelineService.ResetStuckStepAsync(projectId, ct);
        return StatusCode(StatusCodes.Status200OK, ApiResponse<ProjectDetailDto>.Ok(updated, "Project state reset to IDLE successfully.", StatusCodes.Status200OK));
    }
}
