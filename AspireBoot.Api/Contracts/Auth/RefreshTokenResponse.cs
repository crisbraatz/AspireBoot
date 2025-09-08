using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;

namespace AspireBoot.Api.Contracts.Auth;

public record RefreshTokenResponse
{
    public required string AuthToken { get; init; }

    public static BaseResponse<RefreshTokenResponse> ConvertFromDto(BaseResponseDto<RefreshTokenResponseDto> dto) =>
        dto.IsFailure
            ? BaseResponse<RefreshTokenResponse>.Failure(dto.ErrorCode, dto.ErrorMessage)
            : BaseResponse<RefreshTokenResponse>.Success(new RefreshTokenResponse
            {
                AuthToken = dto.Data?.Token!
            });
}