using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.ApiService.Contracts.Users;

public sealed class ListUserResponse : BaseList
{
    public string? Email { get; init; }

    public static BaseListResponse<ListUserResponse> ConvertFromDto(BaseListResponseDto<ListUserResponseDto> response)
        => new()
        {
            Data = response.Data.Select(x => new ListUserResponse
            {
                Email = x.Email,
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy,
                Active = x.Active
            }),
            CurrentPage = response.CurrentPage,
            TotalPages = response.TotalPages,
            TotalItems = response.TotalItems
        };
}
