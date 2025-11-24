using AspireBoot.ApiService.Services.Users;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories;
using FluentAssertions;

namespace AspireBoot.Tests.Integration.Api.Services.Auth;

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
    public async Task ListByShouldListById()
    {
        User userA = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789");
        User userB = new("EXAMPLETWO@EMAIL.COM", "abc123-DEF456-ghi789");
        await _usersRepository.InsertManyAsync(new List<User> { userA, userB });
        await _integrationTestsFixture.CommitAsync();
        ListUsersRequestDto request = new() { Id = userA.Id };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListByAsync(request, _integrationTestsFixture.CancellationToken);

        response.Data.Should().NotBeNullOrEmpty();
        response.CurrentPage.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.TotalItems.Should().Be(1);
        ListUserResponseDto first = response.Data.First();
        first.Email.Should().Be(userA.Email);
        first.Id.Should().Be(userA.Id);
        first.CreatedAt.Should().BeCloseTo(userA.CreatedAt, TimeSpan.FromMilliseconds(100));
        first.CreatedBy.Should().Be(userA.CreatedBy);
        first.UpdatedAt.Should().BeCloseTo(userA.UpdatedAt, TimeSpan.FromMilliseconds(100));
        first.UpdatedBy.Should().Be(userA.UpdatedBy);
        first.Active.Should().Be(userA.Active);
    }

    [Fact]
    public async Task ListByShouldReturnEmptyWhenIdNotFound()
    {
        User userA = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789");
        User userB = new("EXAMPLETWO@EMAIL.COM", "abc123-DEF456-ghi789");
        await _usersRepository.InsertManyAsync(new List<User> { userA, userB });
        await _integrationTestsFixture.CommitAsync();
        ListUsersRequestDto request = new() { Id = Guid.CreateVersion7() };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListByAsync(request, _integrationTestsFixture.CancellationToken);

        response.Data.Should().BeEmpty();
        response.CurrentPage.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task ListByShouldListByEmailOrderedByEmailAscending()
    {
        User userA = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789");
        User userB = new("EXAMPLETWO@EMAIL.COM", "abc123-DEF456-ghi789");
        await _usersRepository.InsertManyAsync(new List<User> { userA, userB });
        await _integrationTestsFixture.CommitAsync();
        ListUsersRequestDto request = new()
        {
            Email = "@EMAIL.COM", OrderBy = new Dictionary<string, bool> { { "EMAIL", true } }
        };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListByAsync(request, _integrationTestsFixture.CancellationToken);

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
    public async Task ListByShouldListByEmailOrderedByEmailDescending()
    {
        User userA = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789");
        User userB = new("EXAMPLETWO@EMAIL.COM", "abc123-DEF456-ghi789");
        await _usersRepository.InsertManyAsync(new List<User> { userA, userB });
        await _integrationTestsFixture.CommitAsync();
        ListUsersRequestDto request = new()
        {
            Email = "@EMAIL.COM", OrderBy = new Dictionary<string, bool> { { "EMAIL", false } }
        };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListByAsync(request, _integrationTestsFixture.CancellationToken);

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
    public async Task ListByShouldReturnEmptyWhenEmailNotFound()
    {
        User userA = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789");
        User userB = new("EXAMPLE2@EMAIL.COM", "abc123-DEF456-ghi789");
        await _usersRepository.InsertManyAsync(new List<User> { userA, userB });
        await _integrationTestsFixture.CommitAsync();
        ListUsersRequestDto request = new() { Email = "EXAMPLE3@EMAIL.COM" };

        BaseListResponseDto<ListUserResponseDto> response =
            await _usersService.ListByAsync(request, _integrationTestsFixture.CancellationToken);

        response.Data.Should().BeEmpty();
        response.CurrentPage.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.TotalItems.Should().Be(0);
    }
}
