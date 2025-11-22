using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;

namespace AspireBoot.ApiService.Contracts.Tokens;

public sealed class RefreshTokenResponse(string token)
{
    public string Token { get; } = token;

    public static BaseResponse<RefreshTokenResponse> ConvertFromDto(BaseResponseDto<RefreshTokenResponseDto> response)
        => response.IsFailure
            ? BaseResponse<RefreshTokenResponse>.Failure(response.ErrorCode, response.ErrorMessage)
            : BaseResponse<RefreshTokenResponse>.Success(new RefreshTokenResponse(response.Data?.Token!));
}
