namespace AspireBoot.Domain.DTOs.Users;

public class ListUsersRequestDto
{
    public string? Email { get; init; }
    public int CurrentPage { get; init; } = 1;
    public int Size { get; init; } = 10;
    public string? SortBy { get; init; } = "email";
    public bool SortDescending { get; init; }
}
