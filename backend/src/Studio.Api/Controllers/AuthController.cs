using Microsoft.AspNetCore.Mvc;
using Studio.Api.Application.Common;
using Studio.Api.Application.DTOs;
using Studio.Api.Application.Services;

namespace Studio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IProjectService _projectService;

    public AuthController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var user = await _projectService.SignInAsync(request, ct);
        return StatusCode(StatusCodes.Status200OK, ApiResponse<UserDto>.Ok(user, "Signed in successfully.", StatusCodes.Status200OK));
    }
}
