using System.Text.Json;
using AspireBoot.Domain;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Infrastructure.Postgres.Repositories.Users;
using AspireBoot.Infrastructure.Rabbit;

namespace AspireBoot.ApiService.Services.Users;

public class UsersService(IUsersRepository usersRepository, BasePublisher basePublisher) : IUsersService
{
    public async Task<BaseListResponseDto<ListUserResponseDto>> ListByAsync(
        ListUsersRequestDto request, CancellationToken cancellationToken = default)
    {
        await basePublisher
            .PublishAsync(
                AppSettings.RabbitUserListRequestsCounterConsumerExchange,
                JsonSerializer.Serialize(request),
                cancellationToken)
            .ConfigureAwait(false);

        if (request.Id is not null)
        {
            ListUserResponseDto? response = await usersRepository.ProjectOneByAsync(
                    x => x.Id == request.Id,
                    x => new ListUserResponseDto
                    {
                        Email = x.Email,
                        Id = x.Id,
                        CreatedAt = x.CreatedAt,
                        CreatedBy = x.CreatedBy,
                        UpdatedAt = x.UpdatedAt,
                        UpdatedBy = x.UpdatedBy,
                        Active = x.Active
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return response is null
                ? new BaseListResponseDto<ListUserResponseDto>()
                : new BaseListResponseDto<ListUserResponseDto>(response);
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
            request.Filters.Add(x => x.Email.Contains(request.Email));

        (IEnumerable<ListUserResponseDto> data, int totalItems) = await usersRepository.ProjectManyByAsync(
                request,
                x => new ListUserResponseDto
                {
                    Email = x.Email,
                    Id = x.Id,
                    CreatedAt = x.CreatedAt,
                    CreatedBy = x.CreatedBy,
                    UpdatedAt = x.UpdatedAt,
                    UpdatedBy = x.UpdatedBy,
                    Active = x.Active
                },
                cancellationToken)
            .ConfigureAwait(false);

        return new BaseListResponseDto<ListUserResponseDto>(data, request.CurrentPage, request.Size, totalItems);
    }
}
