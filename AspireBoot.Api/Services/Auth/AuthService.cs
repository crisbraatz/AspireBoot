using AspireBoot.Api.Helpers;
using AspireBoot.Api.Validators;
using AspireBoot.Domain;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories;
using AspireBoot.Infrastructure.Rabbit;
using AspireBoot.Infrastructure.Redis;

namespace AspireBoot.Api.Services.Auth;

public class AuthService(
    IBaseEntityRepository<User> usersRepository,
    IRedisRepository redisRepository,
    BasePublisher publisher,
    ILogger<AuthService> logger)
    : IAuthService
{
    private const string RedisPrefix = "RefreshToken";

    public async Task<BaseResponseDto<RefreshTokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) && request.IsRefresh
            || !string.IsNullOrWhiteSpace(request.Token) && !request.IsRefresh)
        {
            logger.LogInformation("{class}.{method} - Unauthorized access.",
                nameof(AuthService), nameof(RefreshTokenAsync));

            return BaseResponseDto<RefreshTokenResponseDto>.Failure(401, "Unauthorized access.");
        }

        string? refreshTokenDb = null;

        if (request.IsRefresh)
        {
            refreshTokenDb = await redisRepository.GetValueFromKeyAsync(RedisPrefix, request.Token!, token);
            if (refreshTokenDb is null)
            {
                logger.LogInformation("{class}.{method} - Token expired.",
                    nameof(AuthService), nameof(RefreshTokenAsync));

                return BaseResponseDto<RefreshTokenResponseDto>.Failure(401, "Token expired.");
            }
        }

        var refreshToken = TokenHelper.GenerateRefreshJwtFor();

        if (refreshTokenDb is not null)
            await redisRepository.RemoveKeyAsync(RedisPrefix, refreshTokenDb, token);

        await redisRepository.SetKeyAsync(RedisPrefix, refreshToken, request.RequestedBy!, token);

        return BaseResponseDto<RefreshTokenResponseDto>.Success(
            new RefreshTokenResponseDto(refreshToken, TokenHelper.GenerateJwtFor(request.RequestedBy!)));
    }

    public async Task<BaseResponseDto> SignInAsync(SignInUserRequestDto request, CancellationToken token = default)
    {
        var emailValidatorResponse = EmailValidator.ValidateFormat(request.Email);
        if (emailValidatorResponse.IsFailure)
        {
            logger.LogInformation("{class}.{method} - Invalid email {email} format.",
                nameof(AuthService), nameof(SignInAsync), request.Email);

            return BaseResponseDto.Failure(400, $"Invalid email {request.Email} format.");
        }

        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            logger.LogInformation("{class}.{method} - Authenticated email {email} can not request user sign in.",
                nameof(AuthService), nameof(SignInAsync), request.RequestedBy);

            return BaseResponseDto.Failure(
                401, $"Authenticated email {request.RequestedBy} can not request user sign in.");
        }

        var passwordValidatorResponse = PasswordValidator.ValidateFormat(request.Password);
        if (passwordValidatorResponse.IsFailure)
        {
            logger.LogInformation("{class}.{method} - Invalid password format for email {email} .",
                nameof(AuthService), nameof(SignInAsync), request.Email);

            return BaseResponseDto.Failure(400, $"Invalid password format for email {request.Email} .");
        }

        var email = request.Email.ToLowerInvariant();

        var userDb = await usersRepository.SelectOneByAsync(x => x.Email == email && x.Active, token: token);
        if (userDb is null)
        {
            logger.LogInformation("{class}.{method} - Email {email} not found.",
                nameof(AuthService), nameof(SignInAsync), email);

            return BaseResponseDto.Failure(404, $"Email {email} not found.");
        }

        if (!userDb.Email.IsMatch(userDb.Password, request.Password))
        {
            logger.LogInformation("{class}.{method} - Invalid password for email {email} .",
                nameof(AuthService), nameof(SignInAsync), email);

            return BaseResponseDto.Failure(400, $"Invalid password for email {email} .");
        }

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto> SignOutAsync(RefreshTokenRequestDto request, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            logger.LogInformation("{class}.{method} - Unauthenticated email can not request user sign out.",
                nameof(AuthService), nameof(SignOutAsync));

            return BaseResponseDto.Failure(401, "Unauthenticated email can not request user sign out.");
        }

        await redisRepository.RemoveKeyAsync(RedisPrefix, request.Token!, token);

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto> SignUpAsync(SignUpUserRequestDto request, CancellationToken token = default)
    {
        var emailValidatorResponse = EmailValidator.ValidateFormat(request.Email);
        if (emailValidatorResponse.IsFailure)
        {
            logger.LogInformation("{class}.{method} - Invalid email {email} format.",
                nameof(AuthService), nameof(SignUpAsync), request.Email);

            return BaseResponseDto.Failure(400, $"Invalid email {request.Email} format.");
        }

        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            logger.LogInformation("{class}.{method} - Authenticated email {email} can not request user sign up.",
                nameof(AuthService), nameof(SignUpAsync), request.RequestedBy);

            return BaseResponseDto.Failure(
                401, $"Authenticated email {request.RequestedBy} can not request user sign up.");
        }

        var passwordValidatorResponse = PasswordValidator.ValidateFormat(request.Password);
        if (passwordValidatorResponse.IsFailure)
        {
            logger.LogInformation("{class}.{method} - Invalid password format for email {email} .",
                nameof(AuthService), nameof(SignUpAsync), request.Email);

            return BaseResponseDto.Failure(400, $"Invalid password format for email {request.Email} .");
        }

        var email = request.Email.ToLowerInvariant();

        if (await usersRepository.ExistsByAsync(x => x.Email == email, token))
        {
            logger.LogInformation("{class}.{method} - Email {email} already used.",
                nameof(AuthService), nameof(SignUpAsync), email);

            return BaseResponseDto.Failure(409, $"Email {email} already used.");
        }

        await usersRepository.InsertOneAsync(new User(email, email.GetHashedPassword(request.Password)), token);
        await usersRepository.Commit(token);

        return BaseResponseDto.Success();
    }

    public async Task<BaseResponseDto<string>> TestAsync(CancellationToken token = default)
    {
        const string pong = "Pong";

        await publisher.PublishAsync(AppSettings.RabbitPingConsumerExchange, pong, token);

        return BaseResponseDto<string>.Success(pong);
    }

    public async Task<ValidatorResponse> ValidateJwtAsync(string? request, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            logger.LogInformation("{class}.{method} - Claim not found in request headers authorization.",
                nameof(AuthService), nameof(ValidateJwtAsync));

            return ValidatorResponse.Failure(404, "Claim not found in request headers authorization.");
        }

        if (!await usersRepository.ExistsByAsync(x => x.Email == request && x.Active, token))
        {
            logger.LogInformation("{class}.{method} - Unauthorized access",
                nameof(AuthService), nameof(ValidateJwtAsync));

            return ValidatorResponse.Failure(401, "Unauthorized access.");
        }

        return ValidatorResponse.Success();
    }
}