using System.ComponentModel.DataAnnotations;

namespace Studio.Api.Application.DTOs;

public record SignInRequest(
    [Required, MinLength(1)] string Name,
    [Required, EmailAddress] string Email
);

public record UserDto(
    string Id,
    string Email,
    string Name,
    DateTime CreatedAt
);
