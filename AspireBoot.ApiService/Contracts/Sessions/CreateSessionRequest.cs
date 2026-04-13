using System.ComponentModel.DataAnnotations;
using AspireBoot.Domain.DTOs.Sessions;

namespace AspireBoot.ApiService.Contracts.Sessions;

public sealed class CreateSessionRequest
{
    [Required] public required string Email { get; init; }
    [Required] public required string Password { get; init; }

    public CreateSessionRequestDto ConvertToDto(string? requestedBy) => new(Email, Password, requestedBy);
}
