using System.Text.Json;
using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories.Users;
using AspireBoot.Infrastructure.Rabbit;
using AspireBoot.Infrastructure.Redis;

namespace AspireBoot.ApiService.Services.Users;

public class UsersService(
    IUsersRepository usersRepository,
    IRedisRepository redisRepository,
    BasePublisher basePublisher)
    : IUsersService
{
    private const string RedisPrefix = "SessionsRefreshToken";

    public async Task<BaseResponseDto<RefreshSessionResponseDto>> CreateAsync(
        CreateUserRequestDto request, CancellationToken cancellationToken = default)
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

        if (await usersRepository.ExistsByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false))
            return BaseResponseDto<RefreshSessionResponseDto>.Failure(
                StatusCodes.Status409Conflict, "Email already used.");

        await usersRepository
            .InsertOneAsync(
                new User(request.Email, request.Email.GetHashedPassword(request.Password)), cancellationToken)
            .ConfigureAwait(false);

        string refreshToken = TokenHelper.GenerateRefreshJwtFor();

        await redisRepository
            .SetKeyAsync(RedisPrefix, refreshToken, request.Email, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return BaseResponseDto<RefreshSessionResponseDto>.Success(
            new RefreshSessionResponseDto(refreshToken, TokenHelper.GenerateJwtFor(request.Email)));
    }

    public async Task<BaseListResponseDto<ListUserResponseDto>> ListAsync(
        ListUsersRequestDto request, CancellationToken cancellationToken = default)
    {
        await basePublisher
            .PublishAsync(
                AppSettings.RabbitUserListRequestsCounterConsumerExchange,
                JsonSerializer.Serialize(request),
                cancellationToken)
            .ConfigureAwait(false);

        (IEnumerable<ListUserResponseDto> data, int totalItems) =
            await usersRepository.ListAsync(request, cancellationToken).ConfigureAwait(false);

        return new BaseListResponseDto<ListUserResponseDto>(data, request.CurrentPage, request.Size, totalItems);
    }
}
