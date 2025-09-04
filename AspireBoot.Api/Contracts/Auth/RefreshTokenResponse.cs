using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;

namespace AspireBoot.Api.Contracts.Auth;

public record RefreshTokenResponse
{
    public required string AuthToken { get; init; }

    public static BaseResponse<RefreshTokenResponse> ConvertFromDto(BaseResponseDto<RefreshTokenResponseDto> dto) =>
        new()
        {
            Data = new RefreshTokenResponse
            {
                AuthToken = dto.Data?.Token!
            },
            ErrorCode = dto.ErrorCode,
            ErrorMessage = dto.ErrorMessage,
            IsFailure = dto.IsFailure
        };
}