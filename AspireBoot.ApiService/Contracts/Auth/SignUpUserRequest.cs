using System.ComponentModel.DataAnnotations;
using AspireBoot.Domain.DTOs.Auth;

namespace AspireBoot.ApiService.Contracts.Auth;

public sealed class SignUpUserRequest
{
    [Required] public required string Email { get; init; }

    [Required] public required string Password { get; init; }

    public SignUpUserRequestDto ConvertToDto(string? requestedBy) => new(Email, Password, requestedBy);
}
