using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Services.Sessions;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories;
using AspireBoot.Infrastructure.Redis;
using FluentAssertions;

namespace AspireBoot.Tests.Integration.Api.Services.Sessions;

[Collection("IntegrationTestsCollection")]
public class SessionsServiceTests
{
    private const string RedisPrefix = "SessionsRefreshToken";

    private readonly IntegrationTestsFixture _integrationTestsFixture;
    private readonly IBaseEntityRepository<User> _usersRepository;
    private readonly IRedisRepository _redisRepository;
    private readonly ISessionsService _sessionsService;

    public SessionsServiceTests(IntegrationTestsFixture integrationTestsFixture)
    {
        _integrationTestsFixture = integrationTestsFixture;
        _integrationTestsFixture.ResetAsync().GetAwaiter().GetResult();
        _usersRepository = _integrationTestsFixture.GetRequiredService<IBaseEntityRepository<User>>();
        _redisRepository = _integrationTestsFixture.GetRequiredService<IRedisRepository>();
        _sessionsService = _integrationTestsFixture.GetRequiredService<ISessionsService>();
    }

    [Fact]
    public async Task CreateShouldCreate()
    {
        CreateSessionRequestDto request =
            new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword(request.Password)),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateShouldReturnUnauthorizedAccessWhenInvalidRequest()
    {
        CreateSessionRequestDto request =
            new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", IntegrationTestsFixture.RequestedBy);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task CreateShouldReturnInvalidEmailFormatWhenInvalidRequest()
    {
        string email = string.Empty;
        CreateSessionRequestDto request = new(email, "abc123-DEF456-ghi789", email);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid email format.");
    }

    [Fact]
    public async Task CreateShouldReturnInvalidPasswordFormatWhenInvalidRequest()
    {
        CreateSessionRequestDto request = new(IntegrationTestsFixture.RequestedBy, "StrongPassword", string.Empty);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password format.");
    }

    [Fact]
    public async Task CreateShouldReturnUserNotFoundWhenInvalidRequest()
    {
        CreateSessionRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789",
            string.Empty);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(404);
        response.ErrorMessage.Should().Be("User not found.");
    }

    [Fact]
    public async Task CreateShouldReturnInvalidPasswordWhenInvalidRequest()
    {
        CreateSessionRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789",
            string.Empty);
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("abc123-DEF456-ghi789!")),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.CreateAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password.");
    }

    [Fact]
    public async Task DeleteShouldDelete()
    {
        string token = Guid.CreateVersion7().ToString();
        await _redisRepository.SetKeyAsync(
            RedisPrefix,
            token,
            IntegrationTestsFixture.RequestedBy,
            cancellationToken: _integrationTestsFixture.CancellationToken);
        DeleteSessionRequestDto request = new(token, IntegrationTestsFixture.RequestedBy);

        BaseResponseDto response =
            await _sessionsService.DeleteAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DeleteShouldReturnUnauthorizedAccessWhenInvalidRequest()
    {
        DeleteSessionRequestDto request = new(Guid.CreateVersion7().ToString(), string.Empty);

        BaseResponseDto response =
            await _sessionsService.DeleteAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task RefreshShouldRefresh()
    {
        string token = Guid.CreateVersion7().ToString();
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("abc123-DEF456-ghi789")),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();
        await _redisRepository.SetKeyAsync(
            RedisPrefix,
            token,
            IntegrationTestsFixture.RequestedBy,
            cancellationToken: _integrationTestsFixture.CancellationToken);
        RefreshSessionRequestDto request = new(token, IntegrationTestsFixture.RequestedBy);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.RefreshAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
        response.Data.Should().NotBeNull();
        response.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.Data.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RefreshShouldReturnUnauthorizedAccessWhenInvalidRequest(string? token)
    {
        RefreshSessionRequestDto request = new(token, IntegrationTestsFixture.RequestedBy);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.RefreshAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task RefreshShouldReturnUnauthorizedAccessWhenTokenExpired()
    {
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("abc123-DEF456-ghi789")),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();
        RefreshSessionRequestDto request = new(Guid.CreateVersion7().ToString(), IntegrationTestsFixture.RequestedBy);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.RefreshAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task RefreshShouldReturnUnauthorizedAccessWhenEmailNotFound()
    {
        string token = Guid.CreateVersion7().ToString();
        await _redisRepository.SetKeyAsync(
            RedisPrefix,
            token,
            IntegrationTestsFixture.RequestedBy,
            cancellationToken: _integrationTestsFixture.CancellationToken);
        RefreshSessionRequestDto request = new(token, IntegrationTestsFixture.RequestedBy);

        BaseResponseDto<RefreshSessionResponseDto> response =
            await _sessionsService.RefreshAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
        response.Data.Should().BeNull();
    }
}
