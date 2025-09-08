using AspireBoot.Api.Helpers;
using AspireBoot.Api.Services.Auth;
using AspireBoot.Domain.DTOs.Tokens;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories;
using AspireBoot.Infrastructure.Redis;
using FluentAssertions;

namespace AspireBoot.Tests.Integration.Api.Services.Auth;

[Collection("IntegrationTestsCollection")]
public class AuthServiceTests
{
    private readonly IntegrationTestsFixture _fixture;
    private readonly IBaseEntityRepository<User> _usersRepository;
    private readonly IRedisRepository _redisRepository;
    private readonly IAuthService _authService;

    public AuthServiceTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetAsync().GetAwaiter().GetResult();
        _usersRepository = _fixture.GetRequiredService<IBaseEntityRepository<User>>();
        _redisRepository = _fixture.GetRequiredService<IRedisRepository>();
        _authService = _fixture.GetRequiredService<IAuthService>();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("a3a85459-7b6c-49ec-9440-d6ddb14aef95", true)]
    public async Task RefreshToken_Should_refresh_token(string? token, bool isRefresh)
    {
        await _redisRepository.SetKeyAsync("RefreshToken", token!, IntegrationTestsFixture.RequestedBy, _fixture.Token);
        var request = new RefreshTokenRequestDto(token, IntegrationTestsFixture.RequestedBy, isRefresh);

        var response = await _authService.RefreshTokenAsync(request, _fixture.Token);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
        response.Data.Should().NotBeNull();
        response.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.Data.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("token", false)]
    public async Task RefreshToken_Should_return_unauthorized_access_when_invalid_request(string? token, bool isRefresh)
    {
        var request = new RefreshTokenRequestDto(token, IntegrationTestsFixture.RequestedBy, isRefresh);

        var response = await _authService.RefreshTokenAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_Should_return_token_expired_when_invalid_request()
    {
        var request =
            new RefreshTokenRequestDto("a3a85459-7b6c-49ec-9440-d6ddb14aef95", IntegrationTestsFixture.RequestedBy);

        var response = await _authService.RefreshTokenAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Token expired.");
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task SignIn_Should_sign_in()
    {
        var request = new SignInUserRequestDto(IntegrationTestsFixture.RequestedBy, "Password123", string.Empty);
        await _usersRepository.InsertOneAsync(new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword(request.Password)),
            _fixture.Token);
        await _fixture.CommitAsync();

        var response = await _authService.SignInAsync(request, _fixture.Token);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignIn_Should_return_invalid_email_format_when_invalid_request()
    {
        var email = string.Empty;
        var request = new SignInUserRequestDto(email, "Password123", email);

        var response = await _authService.SignInAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be($"Invalid email {request.Email} format.");
    }

    [Fact]
    public async Task SignIn_Should_return_authenticated_email_can_not_request_user_signin_when_invalid_request()
    {
        var request = new SignInUserRequestDto(
            IntegrationTestsFixture.RequestedBy, "Password123", IntegrationTestsFixture.RequestedBy);

        var response = await _authService.SignInAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be($"Authenticated email {request.RequestedBy} can not request user sign in.");
    }

    [Fact]
    public async Task SignIn_Should_return_invalid_password_format_for_email_when_invalid_request()
    {
        var request = new SignInUserRequestDto(IntegrationTestsFixture.RequestedBy, "Password", string.Empty);

        var response = await _authService.SignInAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be($"Invalid password format for email {request.Email} .");
    }

    [Fact]
    public async Task SignIn_Should_return_email_not_found_when_invalid_request()
    {
        var request = new SignInUserRequestDto(IntegrationTestsFixture.RequestedBy, "Password123", string.Empty);

        var response = await _authService.SignInAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(404);
        response.ErrorMessage.Should().Be($"Email {request.Email} not found.");
    }

    [Fact]
    public async Task SignIn_Should_return_invalid_password_for_email_when_invalid_request()
    {
        var request = new SignInUserRequestDto(IntegrationTestsFixture.RequestedBy, "Password123", string.Empty);
        await _usersRepository.InsertOneAsync(new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("Password1234")),
            _fixture.Token);
        await _fixture.CommitAsync();

        var response = await _authService.SignInAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be($"Invalid password for email {request.Email} .");
    }

    [Fact]
    public async Task SignOut_Should_sign_out()
    {
        const string token = "a3a85459-7b6c-49ec-9440-d6ddb14aef95";
        await _redisRepository.SetKeyAsync("RefreshToken", token, IntegrationTestsFixture.RequestedBy, _fixture.Token);
        var request = new RefreshTokenRequestDto(token, IntegrationTestsFixture.RequestedBy);

        var response = await _authService.SignOutAsync(request, _fixture.Token);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignOut_Should_return_unauthenticated_email_can_not_request_user_sign_out_when_invalid_request()
    {
        var request = new RefreshTokenRequestDto("a3a85459-7b6c-49ec-9440-d6ddb14aef95", string.Empty);

        var response = await _authService.SignOutAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthenticated email can not request user sign out.");
    }

    [Fact]
    public async Task SignUp_Should_sign_up()
    {
        var request = new SignUpUserRequestDto(IntegrationTestsFixture.RequestedBy, "Password123", string.Empty);

        var response = await _authService.SignUpAsync(request, _fixture.Token);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignUp_Should_return_invalid_email_format_when_invalid_request()
    {
        var request = new SignUpUserRequestDto(string.Empty, "Password123", string.Empty);

        var response = await _authService.SignUpAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be($"Invalid email {request.Email} format.");
    }

    [Fact]
    public async Task SignUp_Should_return_authenticated_email_can_not_request_user_sign_up_when_invalid_request()
    {
        var request = new SignUpUserRequestDto(
            IntegrationTestsFixture.RequestedBy, "Password123", IntegrationTestsFixture.RequestedBy);

        var response = await _authService.SignUpAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be($"Authenticated email {request.RequestedBy} can not request user sign up.");
    }

    [Fact]
    public async Task SignUp_Should_return_invalid_password_format_for_email_when_invalid_request()
    {
        var request = new SignUpUserRequestDto(
            IntegrationTestsFixture.RequestedBy, "Password", string.Empty);

        var response = await _authService.SignUpAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be($"Invalid password format for email {request.Email} .");
    }

    [Fact]
    public async Task SignUp_Should_return_email_already_used_when_invalid_request()
    {
        var request = new SignUpUserRequestDto(
            IntegrationTestsFixture.RequestedBy, "Password123", string.Empty);
        await _usersRepository.InsertOneAsync(new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("Password123")),
            _fixture.Token);
        await _fixture.CommitAsync();

        var response = await _authService.SignUpAsync(request, _fixture.Token);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(409);
        response.ErrorMessage.Should().Be($"Email {request.Email} already used.");
    }
}