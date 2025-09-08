using System.ComponentModel.DataAnnotations;
using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.Api.Contracts.Auth;

public record SignInRequest
{
    [Required] public required string Email { get; init; }

    [Required] public required string Password { get; init; }

    public SignInUserRequestDto ConvertToDto(string? requestedBy) => new(Email, Password, requestedBy);
}