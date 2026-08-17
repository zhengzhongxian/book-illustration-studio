using Microsoft.AspNetCore.Mvc;
using Studio.Api.Application.Common;
using Studio.Api.Application.Common.Exceptions;
using Studio.Api.Application.DTOs;
using Studio.Api.Application.Services;

namespace Studio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserProjects([FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ValidationException("userId query parameter is required.");

        var projects = await _projectService.GetUserProjectsAsync(userId, ct);
        return StatusCode(StatusCodes.Status200OK, ApiResponse<List<ProjectSummaryDto>>.Ok(projects, null, StatusCodes.Status200OK));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProjectById(string id, CancellationToken ct)
    {
        var project = await _projectService.GetProjectByIdAsync(id, ct);
        if (project == null) throw new NotFoundException("Project", id);

        return StatusCode(StatusCodes.Status200OK, ApiResponse<ProjectDetailDto>.Ok(project, null, StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var project = await _projectService.CreateProjectAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProjectDetailDto>.Ok(project, "Project created successfully.", StatusCodes.Status201Created));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(string id, [FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ValidationException("userId query parameter is required.");

        var deleted = await _projectService.DeleteProjectAsync(id, userId, ct);
        if (!deleted) throw new NotFoundException("Project", id);

        return StatusCode(StatusCodes.Status200OK, ApiResponse.Ok("Project deleted successfully.", StatusCodes.Status200OK));
    }
}
