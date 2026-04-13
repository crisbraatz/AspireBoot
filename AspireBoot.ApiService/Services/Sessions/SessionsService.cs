using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories.Users;
using AspireBoot.Infrastructure.Redis;

namespace AspireBoot.ApiService.Services.Sessions;

public sealed class SessionsService(IUsersRepository usersRepository, IRedisRepository redisRepository)
    : ISessionsService
{
    private const string RedisPrefix = "SessionsRefreshToken";

    public async Task<BaseResponseDto<RefreshSessionResponseDto>> CreateAsync(
        CreateSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(
                StatusCodes.Status401Unauthorized, "Unauthorized access.");

        ValidatorResponse validatorResponse = EmailValidator.ValidateFormat(request.Email);
        if (validatorResponse.IsFailure)
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(
                validatorResponse.ErrorCode, validatorResponse.ErrorMessage);

        validatorResponse = PasswordValidator.ValidateFormat(request.Password);
        if (validatorResponse.IsFailure)
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(
                validatorResponse.ErrorCode, validatorResponse.ErrorMessage);

        User? userDb = await usersRepository
            .SelectOneByAsync(x => x.Email == request.Email && x.Active, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (userDb is null)
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(StatusCodes.Status404NotFound, "User not found.");

        if (!userDb.Email.IsMatch(userDb.Password, request.Password))
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(
                StatusCodes.Status400BadRequest, "Invalid password.");

        string refreshToken = TokenHelper.GenerateRefreshJwtFor();

        await redisRepository
            .SetKeyAsync(RedisPrefix, refreshToken, request.Email, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return BaseResponseDto<RefreshSessionResponseDto>.Success(
            new RefreshSessionResponseDto(refreshToken, TokenHelper.GenerateJwtFor(request.Email)));
    }

    public async Task<BaseResponseDto> DeleteAsync(
        DeleteSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RequestedBy))
            return BaseResponseDto.Failure(StatusCodes.Status401Unauthorized, "Unauthorized access.");

        if (!string.IsNullOrWhiteSpace(request.Token))
            await redisRepository.RemoveKeyAsync(RedisPrefix, request.Token, cancellationToken).ConfigureAwait(false);

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto<RefreshSessionResponseDto>> RefreshAsync(
        RefreshSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(
                StatusCodes.Status401Unauthorized, "Unauthorized access.");

        string? user = await redisRepository
            .GetValueFromKeyAsync(RedisPrefix, request.Token!, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(user) ||
            !await usersRepository.ExistsByEmailAsync(user, cancellationToken).ConfigureAwait(false) ||
            (!string.IsNullOrWhiteSpace(request.RequestedBy) &&
             !user.Equals(request.RequestedBy, StringComparison.OrdinalIgnoreCase)))
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(
                StatusCodes.Status401Unauthorized, "Unauthorized access.");

        string refreshToken = TokenHelper.GenerateRefreshJwtFor();

        if (!string.IsNullOrWhiteSpace(request.Token))
            await redisRepository.RemoveKeyAsync(RedisPrefix, request.Token, cancellationToken).ConfigureAwait(false);

        await redisRepository
            .SetKeyAsync(RedisPrefix, refreshToken, user, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return BaseResponseDto<RefreshSessionResponseDto>.Success(
            new RefreshSessionResponseDto(refreshToken, TokenHelper.GenerateJwtFor(user)));
    }
}
