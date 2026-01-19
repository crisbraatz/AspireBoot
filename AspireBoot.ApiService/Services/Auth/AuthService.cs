using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Auth;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Domain.Extensions;
using AspireBoot.Infrastructure.Postgres.Repositories.Users;
using AspireBoot.Infrastructure.Redis;

namespace AspireBoot.ApiService.Services.Auth;

public sealed class AuthService(
    IUsersRepository usersRepository,
    IRedisRepository redisRepository,
    ILogger<AuthService> logger)
    : IAuthService
{
    private const string RedisPrefix = "AuthRefreshToken";

    public async Task<BaseResponseDto<RefreshTokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) && request.IsRefresh ||
            !string.IsNullOrWhiteSpace(request.Token) && !request.IsRefresh)
        {
            const string errorMessage = "Unauthorized access.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto<RefreshTokenResponseDto>.Failure(401, errorMessage);
        }

        string? authRefreshTokenDb = null;

        if (request.IsRefresh)
        {
            authRefreshTokenDb = await redisRepository
                .GetValueFromKeyAsync(RedisPrefix, request.Token!, cancellationToken)
                .ConfigureAwait(false);
            if (authRefreshTokenDb is null)
            {
                const string errorMessage = "Token expired.";

                using (logger.BeginScope(nameof(AuthService)))
                    LoggerMessageExtension.LogServiceError(logger, errorMessage);

                return BaseResponseDto<RefreshTokenResponseDto>.Failure(401, errorMessage);
            }
        }

        string authRefreshToken = TokenHelper.GenerateRefreshJwtFor();

        if (authRefreshTokenDb is not null)
            await redisRepository
                .RemoveKeyAsync(RedisPrefix, authRefreshTokenDb, cancellationToken)
                .ConfigureAwait(false);

        await redisRepository
            .SetKeyAsync(RedisPrefix, authRefreshToken, request.RequestedBy!, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return BaseResponseDto<RefreshTokenResponseDto>.Success(
            new RefreshTokenResponseDto(authRefreshToken, TokenHelper.GenerateJwtFor(request.RequestedBy!)));
    }

    public async Task<BaseResponseDto> SignInAsync(
        SignInUserRequestDto request, CancellationToken cancellationToken = default)
    {
        if (EmailValidator.ValidateFormat(request.Email).IsFailure)
        {
            const string errorMessage = "Invalid email format.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(400, errorMessage);
        }

        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            const string errorMessage = "Authenticated email can not request user sign in.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(401, errorMessage);
        }

        if (PasswordValidator.ValidateFormat(request.Password).IsFailure)
        {
            const string errorMessage = "Invalid password format.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(400, errorMessage);
        }

        User? userDb = await usersRepository
            .SelectOneByAsync(x => x.Email == request.Email && x.Active, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (userDb is null)
        {
            const string errorMessage = "Email not found.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(404, errorMessage);
        }

        if (!userDb.Email.IsMatch(userDb.Password, request.Password))
        {
            const string errorMessage = "Invalid password.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(400, errorMessage);
        }

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto> SignOutAsync(
        RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            const string errorMessage = "Unauthenticated email can not request user sign out.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(401, errorMessage);
        }

        await redisRepository.RemoveKeyAsync(RedisPrefix, request.Token!, cancellationToken).ConfigureAwait(false);

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto> SignUpAsync(
        SignUpUserRequestDto request, CancellationToken cancellationToken = default)
    {
        if (EmailValidator.ValidateFormat(request.Email).IsFailure)
        {
            const string errorMessage = "Invalid email format.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(400, errorMessage);
        }

        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            const string errorMessage = "Authenticated email can not request user sign up.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(401, errorMessage);
        }

        if (PasswordValidator.ValidateFormat(request.Password).IsFailure)
        {
            const string errorMessage = "Invalid password format for email.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(400, errorMessage);
        }

        if (await usersRepository.ExistsByAsync(request.Email, cancellationToken).ConfigureAwait(false))
        {
            const string errorMessage = "Email already used.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return BaseResponseDto.Failure(409, errorMessage);
        }

        await usersRepository
            .InsertOneAsync(
                new User(request.Email, request.Email.GetHashedPassword(request.Password)), cancellationToken)
            .ConfigureAwait(false);
        await usersRepository.CommitAsync(cancellationToken).ConfigureAwait(false);

        return BaseResponseDto.Success();
    }

    public async Task<ValidatorResponse> ValidateJwtAsync(
        string? request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            const string errorMessage = "Claim not found in request headers authorization.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return ValidatorResponse.Failure(404, errorMessage);
        }

        if (!await usersRepository.ExistsByAsync(request, cancellationToken).ConfigureAwait(false))
        {
            const string errorMessage = "Unauthorized access.";

            using (logger.BeginScope(nameof(AuthService)))
                LoggerMessageExtension.LogServiceError(logger, errorMessage);

            return ValidatorResponse.Failure(401, errorMessage);
        }

        return ValidatorResponse.Success();
    }
}
