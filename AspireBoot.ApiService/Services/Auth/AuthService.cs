using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Auth;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories.Users;
using AspireBoot.Infrastructure.Redis;

namespace AspireBoot.ApiService.Services.Auth;

public sealed class AuthService(IUsersRepository usersRepository, IRedisRepository redisRepository) : IAuthService
{
    private const string RedisPrefix = "AuthRefreshToken";

    public async Task<BaseResponseDto<RefreshTokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) && request.IsRefresh ||
            !string.IsNullOrWhiteSpace(request.Token) && !request.IsRefresh)
            return BaseResponseDto<RefreshTokenResponseDto>.Failure(401, "Unauthorized access.");

        string? authRefreshTokenDb = null;

        if (request.IsRefresh)
        {
            authRefreshTokenDb = await redisRepository
                .GetValueFromKeyAsync(RedisPrefix, request.Token!, cancellationToken).ConfigureAwait(false);
            if (authRefreshTokenDb is null)
                return BaseResponseDto<RefreshTokenResponseDto>.Failure(401, "Token expired.");
        }

        string authRefreshToken = TokenHelper.GenerateRefreshJwtFor();

        if (authRefreshTokenDb is not null)
            await redisRepository
                .RemoveKeyAsync(RedisPrefix, authRefreshTokenDb, cancellationToken).ConfigureAwait(false);

        await redisRepository
            .SetKeyAsync(RedisPrefix, authRefreshToken, request.RequestedBy!, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return BaseResponseDto<RefreshTokenResponseDto>.Success(
            new RefreshTokenResponseDto(authRefreshToken, TokenHelper.GenerateJwtFor(request.RequestedBy!)));
    }

    public async Task<BaseResponseDto> SignInAsync(
        SignInUserRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatorResponse validatorResponse = EmailValidator.ValidateFormat(request.Email);
        if (validatorResponse.IsFailure)
            return BaseResponseDto.Failure(validatorResponse.ErrorCode, validatorResponse.ErrorMessage);

        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
            return BaseResponseDto.Failure(401, "Authenticated email can not request user sign in.");

        validatorResponse = PasswordValidator.ValidateFormat(request.Password);
        if (validatorResponse.IsFailure)
            return BaseResponseDto.Failure(validatorResponse.ErrorCode, validatorResponse.ErrorMessage);

        User? userDb = await usersRepository
            .SelectOneByAsync(x => x.Email == request.Email && x.Active, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (userDb is null)
            return BaseResponseDto.Failure(404, "Email not found.");

        if (!userDb.Email.IsMatch(userDb.Password, request.Password))
            return BaseResponseDto.Failure(400, "Invalid password.");

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto> SignOutAsync(
        RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RequestedBy))
            return BaseResponseDto.Failure(401, "Unauthenticated email can not request user sign out.");

        await redisRepository.RemoveKeyAsync(RedisPrefix, request.Token!, cancellationToken).ConfigureAwait(false);

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto> SignUpAsync(
        SignUpUserRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatorResponse validatorResponse = EmailValidator.ValidateFormat(request.Email);
        if (validatorResponse.IsFailure)
            return BaseResponseDto.Failure(validatorResponse.ErrorCode, validatorResponse.ErrorMessage);

        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
            return BaseResponseDto.Failure(401, "Authenticated email can not request user sign up.");

        validatorResponse = PasswordValidator.ValidateFormat(request.Password);
        if (validatorResponse.IsFailure)
            return BaseResponseDto.Failure(validatorResponse.ErrorCode, validatorResponse.ErrorMessage);

        if (await usersRepository.ExistsByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false))
            return BaseResponseDto.Failure(409, "Email already used.");

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
            return ValidatorResponse.Failure(404, "Claim not found in request headers authorization.");

        if (!await usersRepository.ExistsByEmailAsync(request, cancellationToken).ConfigureAwait(false))
            return ValidatorResponse.Failure(401, "Unauthorized access.");

        return ValidatorResponse.Success();
    }
}
