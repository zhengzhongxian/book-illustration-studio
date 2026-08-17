using Microsoft.EntityFrameworkCore;
using Studio.Api.Application.DTOs;
using Studio.Api.Domain.Entities;
using Studio.Api.Domain.Enums;
using Studio.Api.Infrastructure.Data;

namespace Studio.Api.Application.Services;

public interface IProjectService
{
    Task<UserDto> SignInAsync(SignInRequest request, CancellationToken ct = default);
    Task<List<ProjectSummaryDto>> GetUserProjectsAsync(string userId, CancellationToken ct = default);
    Task<ProjectDetailDto?> GetProjectByIdAsync(string projectId, CancellationToken ct = default);
    Task<ProjectDetailDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<bool> DeleteProjectAsync(string projectId, string userId, CancellationToken ct = default);
}

public class ProjectService : IProjectService
{
    private readonly StudioDbContext _db;

    public ProjectService(StudioDbContext db)
    {
        _db = db;
    }

    public async Task<UserDto> SignInAsync(SignInRequest request, CancellationToken ct = default)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailNormalized, ct);

        if (user == null)
        {
            user = new User
            {
                Email = emailNormalized,
                Name = request.Name.Trim()
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        else if (user.Name != request.Name.Trim())
        {
            user.Name = request.Name.Trim();
            await _db.SaveChangesAsync(ct);
        }

        return new UserDto(user.Id, user.Email, user.Name, user.CreatedAt);
    }

    public async Task<List<ProjectSummaryDto>> GetUserProjectsAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Projects
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Title,
                p.Status,
                p.StepState,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<ProjectDetailDto?> GetProjectByIdAsync(string projectId, CancellationToken ct = default)
    {
        var p = await _db.Projects
            .AsNoTracking()
            .Include(x => x.Characters.OrderBy(c => c.SortOrder))
            .Include(x => x.Chapters.OrderBy(c => c.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == projectId, ct);

        if (p == null) return null;

        return MapToDetailDto(p);
    }

    public async Task<ProjectDetailDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists) throw new InvalidOperationException("User not found.");

        var project = new Project
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            BookText = request.BookText.Trim(),
            Status = ProjectStatus.CREATED,
            StepState = StepState.IDLE
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return MapToDetailDto(project);
    }

    public async Task<bool> DeleteProjectAsync(string projectId, string userId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);
        if (project == null) return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public static ProjectDetailDto MapToDetailDto(Project p)
    {
        var characters = p.Characters.Select(c => new CharacterDto(
            c.Id,
            c.Name,
            c.Prompt,
            string.IsNullOrEmpty(c.PortraitPath) ? null : $"/api/images/portraits/{c.Id}",
            c.PortraitReady,
            c.SortOrder
        )).ToList();

        var chapters = p.Chapters.Select(c =>
        {
            List<string> charList;
            try
            {
                charList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(c.CharactersJson) ?? new();
            }
            catch
            {
                charList = new();
            }

            return new ChapterDto(
                c.Id,
                c.Name,
                c.Prompt,
                charList,
                string.IsNullOrEmpty(c.IllustrationPath) ? null : $"/api/images/illustrations/{c.Id}",
                c.IllustrationReady,
                c.SortOrder
            );
        }).ToList();

        return new ProjectDetailDto(
            p.Id,
            p.UserId,
            p.Title,
            p.BookText,
            p.Status,
            p.StepState,
            p.LastError,
            p.StepStartedAt,
            p.Style,
            p.CreatedAt,
            p.UpdatedAt,
            characters,
            chapters
        );
    }
}
