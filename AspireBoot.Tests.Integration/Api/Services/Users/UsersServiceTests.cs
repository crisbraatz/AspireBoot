using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Services.Users;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories;
using FluentAssertions;

namespace AspireBoot.Tests.Integration.Api.Services.Users;

[Collection("IntegrationTestsCollection")]
public class UsersServiceTests
{
    private readonly IntegrationTestsFixture _integrationTestsFixture;
    private readonly IBaseEntityRepository<User> _usersRepository;
    private readonly IUsersService _usersService;

    public UsersServiceTests(IntegrationTestsFixture integrationTestsFixture)
    {
        _integrationTestsFixture = integrationTestsFixture;
        _integrationTestsFixture.ResetAsync().GetAwaiter().GetResult();
        _usersRepository = _integrationTestsFixture.GetRequiredService<IBaseEntityRepository<User>>();
        _usersService = _integrationTestsFixture.GetRequiredService<IUsersService>();
    }

    [Fact]
    public async Task CreateShouldCreate()
    {
        CreateUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _usersService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateShouldReturnUnauthorizedAccessWhenInvalidRequest()
    {
        CreateUserRequestDto request =
            new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", IntegrationTestsFixture.RequestedBy);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _usersService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task CreateShouldReturnInvalidEmailFormatWhenInvalidRequest()
    {
        CreateUserRequestDto request = new(string.Empty, "abc123-DEF456-ghi789", string.Empty);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _usersService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid email format.");
    }

    [Fact]
    public async Task CreateShouldReturnInvalidPasswordFormatWhenInvalidRequest()
    {
        CreateUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "StrongPassword", string.Empty);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _usersService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password format.");
    }

    [Fact]
    public async Task CreateShouldReturnEmailAlreadyUsedWhenInvalidRequest()
    {
        CreateUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("abc123-DEF456-ghi789")),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _usersService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(409);
        response.ErrorMessage.Should().Be("Email already used.");
    }

    [Fact]
    public async Task ListShouldListSortedByEmailAscending()
    {
        User userA = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789");
        User userB = new("EXAMPLE2@EMAIL.COM", "abc123-DEF456-ghi789");
        await _usersRepository.InsertManyAsync(new List<User> { userA, userB });
        await _integrationTestsFixture.CommitAsync();
        ListUsersRequestDto request = new() { Email = "@EMAIL.COM", SortBy = "email" };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListAsync(request, _integrationTestsFixture.CancellationToken);

        response.Data.Should().NotBeNullOrEmpty();
        response.CurrentPage.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.TotalItems.Should().Be(2);
        ListUserResponseDto first = response.Data.First();
        first.Email.Should().Be(userA.Email);
        first.Id.Should().Be(userA.Id);
        first.CreatedAt.Should().BeCloseTo(userA.CreatedAt, TimeSpan.FromMilliseconds(100));
        first.CreatedBy.Should().Be(userA.CreatedBy);
        first.UpdatedAt.Should().BeCloseTo(userA.UpdatedAt, TimeSpan.FromMilliseconds(100));
        first.UpdatedBy.Should().Be(userA.UpdatedBy);
        first.Active.Should().Be(userA.Active);
        ListUserResponseDto last = response.Data.Last();
        last.Email.Should().Be(userB.Email);
        last.Id.Should().Be(userB.Id);
        last.CreatedAt.Should().BeCloseTo(userB.CreatedAt, TimeSpan.FromMilliseconds(100));
        last.CreatedBy.Should().Be(userB.CreatedBy);
        last.UpdatedAt.Should().BeCloseTo(userB.UpdatedAt, TimeSpan.FromMilliseconds(100));
        last.UpdatedBy.Should().Be(userB.UpdatedBy);
        last.Active.Should().Be(userB.Active);
    }

    [Fact]
    public async Task ListShouldListSortedByEmailDescending()
    {
        User userA = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789");
        User userB = new("EXAMPLE2@EMAIL.COM", "abc123-DEF456-ghi789");
        await _usersRepository.InsertManyAsync(new List<User> { userA, userB });
        await _integrationTestsFixture.CommitAsync();
        ListUsersRequestDto request = new() { Email = "@EMAIL.COM", SortBy = "email", SortDescending = true };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListAsync(request, _integrationTestsFixture.CancellationToken);

        response.Data.Should().NotBeNullOrEmpty();
        response.CurrentPage.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.TotalItems.Should().Be(2);
        ListUserResponseDto first = response.Data.First();
        first.Email.Should().Be(userB.Email);
        first.Id.Should().Be(userB.Id);
        first.CreatedAt.Should().BeCloseTo(userB.CreatedAt, TimeSpan.FromMilliseconds(100));
        first.CreatedBy.Should().Be(userB.CreatedBy);
        first.UpdatedAt.Should().BeCloseTo(userB.UpdatedAt, TimeSpan.FromMilliseconds(100));
        first.UpdatedBy.Should().Be(userB.UpdatedBy);
        first.Active.Should().Be(userB.Active);
        ListUserResponseDto last = response.Data.Last();
        last.Email.Should().Be(userA.Email);
        last.Id.Should().Be(userA.Id);
        last.CreatedAt.Should().BeCloseTo(userA.CreatedAt, TimeSpan.FromMilliseconds(100));
        last.CreatedBy.Should().Be(userA.CreatedBy);
        last.UpdatedAt.Should().BeCloseTo(userA.UpdatedAt, TimeSpan.FromMilliseconds(100));
        last.UpdatedBy.Should().Be(userA.UpdatedBy);
        last.Active.Should().Be(userA.Active);
    }

    [Fact]
    public async Task ListShouldReturnEmptyWhenEmailNotFound()
    {
        ListUsersRequestDto request = new() { Email = IntegrationTestsFixture.RequestedBy };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListAsync(request, _integrationTestsFixture.CancellationToken);

        response.Data.Should().BeEmpty();
        response.CurrentPage.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.TotalItems.Should().Be(0);
    }
}
