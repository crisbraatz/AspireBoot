using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Services.Auth;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Auth;
using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Repositories;
using AspireBoot.Infrastructure.Redis;
using FluentAssertions;

namespace AspireBoot.Tests.Integration.Api.Services.Auth;

[Collection("IntegrationTestsCollection")]
public class AuthServiceTests
{
    private readonly IntegrationTestsFixture _integrationTestsFixture;
    private readonly IBaseEntityRepository<User> _usersRepository;
    private readonly IRedisRepository _redisRepository;
    private readonly IAuthService _authService;

    public AuthServiceTests(IntegrationTestsFixture integrationTestsFixture)
    {
        _integrationTestsFixture = integrationTestsFixture;
        _integrationTestsFixture.ResetAsync().GetAwaiter().GetResult();
        _usersRepository = _integrationTestsFixture.GetRequiredService<IBaseEntityRepository<User>>();
        _redisRepository = _integrationTestsFixture.GetRequiredService<IRedisRepository>();
        _authService = _integrationTestsFixture.GetRequiredService<IAuthService>();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Token", true)]
    public async Task RefreshTokenShouldRefreshToken(string? token, bool isRefresh)
    {
        if (!string.IsNullOrWhiteSpace(token))
            token = Guid.CreateVersion7().ToString();
        await _redisRepository.SetKeyAsync(
            "AuthRefreshToken",
            token!,
            IntegrationTestsFixture.RequestedBy,
            cancellationToken: _integrationTestsFixture.CancellationToken);
        RefreshTokenRequestDto request = new(token, IntegrationTestsFixture.RequestedBy, isRefresh);

        BaseResponseDto<RefreshTokenResponseDto> response =
            await _authService.RefreshTokenAsync(request, _integrationTestsFixture.CancellationToken);

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
    public async Task RefreshTokenShouldReturnUnauthorizedAccessWhenInvalidRequest(string? token, bool isRefresh)
    {
        RefreshTokenRequestDto request = new(token, IntegrationTestsFixture.RequestedBy, isRefresh);

        BaseResponseDto<RefreshTokenResponseDto> response =
            await _authService.RefreshTokenAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenShouldReturnTokenExpiredWhenInvalidRequest()
    {
        RefreshTokenRequestDto request = new(Guid.CreateVersion7().ToString(), IntegrationTestsFixture.RequestedBy);

        BaseResponseDto<RefreshTokenResponseDto> response =
            await _authService.RefreshTokenAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Token expired.");
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task SignInShouldSignIn()
    {
        SignInUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword(request.Password)),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();

        BaseResponseDto response = await _authService.SignInAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignInShouldReturnInvalidEmailFormatWhenInvalidRequest()
    {
        string email = string.Empty;
        SignInUserRequestDto request = new(email, "abc123-DEF456-ghi789", email);

        BaseResponseDto response = await _authService.SignInAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid email format.");
    }

    [Fact]
    public async Task SignInShouldReturnAuthenticatedEmailCanNotRequestUserSignInWhenInvalidRequest()
    {
        SignInUserRequestDto request = new(
            IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", IntegrationTestsFixture.RequestedBy);

        BaseResponseDto response = await _authService.SignInAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Authenticated email can not request user sign in.");
    }

    [Fact]
    public async Task SignInShouldReturnInvalidPasswordFormatForEmailWhenInvalidRequest()
    {
        SignInUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "StrongPassword", string.Empty);

        BaseResponseDto response = await _authService.SignInAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password format.");
    }

    [Fact]
    public async Task SignInShouldReturnEmailNotFoundWhenInvalidRequest()
    {
        SignInUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);

        BaseResponseDto response = await _authService.SignInAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(404);
        response.ErrorMessage.Should().Be("Email not found.");
    }

    [Fact]
    public async Task SignInShouldReturnInvalidPasswordForEmailWhenInvalidRequest()
    {
        SignInUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("abc123-DEF456-ghi789!")),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();

        BaseResponseDto response = await _authService.SignInAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password.");
    }

    [Fact]
    public async Task SignOutShouldSignOut()
    {
        string token = Guid.CreateVersion7().ToString();
        await _redisRepository.SetKeyAsync(
            "AuthRefreshToken", token, IntegrationTestsFixture.RequestedBy,
            cancellationToken: _integrationTestsFixture.CancellationToken);
        RefreshTokenRequestDto request = new(token, IntegrationTestsFixture.RequestedBy);

        BaseResponseDto response = await _authService.SignOutAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignOutShouldReturnUnauthenticatedEmailCanNotRequestUserSignOutWhenInvalidRequest()
    {
        RefreshTokenRequestDto request = new(Guid.CreateVersion7().ToString(), string.Empty);

        BaseResponseDto response = await _authService.SignOutAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthenticated email can not request user sign out.");
    }

    [Fact]
    public async Task SignUpShouldSignUp()
    {
        SignUpUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);

        BaseResponseDto response = await _authService.SignUpAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignUpShouldReturnInvalidEmailFormatWhenInvalidRequest()
    {
        SignUpUserRequestDto request = new(string.Empty, "abc123-DEF456-ghi789", string.Empty);

        BaseResponseDto response = await _authService.SignUpAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid email format.");
    }

    [Fact]
    public async Task SignUpShouldReturnAuthenticatedEmailCanNotRequestUserSignUpWhenInvalidRequest()
    {
        SignUpUserRequestDto request = new(
            IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", IntegrationTestsFixture.RequestedBy);

        BaseResponseDto response = await _authService.SignUpAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Authenticated email can not request user sign up.");
    }

    [Fact]
    public async Task SignUpShouldReturnInvalidPasswordFormatForEmailWhenInvalidRequest()
    {
        SignUpUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "StrongPassword", string.Empty);

        BaseResponseDto response = await _authService.SignUpAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password format for email.");
    }

    [Fact]
    public async Task SignUpShouldReturnEmailAlreadyUsedWhenInvalidRequest()
    {
        SignUpUserRequestDto request = new(IntegrationTestsFixture.RequestedBy, "abc123-DEF456-ghi789", string.Empty);
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("abc123-DEF456-ghi789!")),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();

        BaseResponseDto response = await _authService.SignUpAsync(request, _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(409);
        response.ErrorMessage.Should().Be("Email already used.");
    }

    [Fact]
    public async Task ValidateJwtShouldValidateJwt()
    {
        await _usersRepository.InsertOneAsync(
            new User(
                IntegrationTestsFixture.RequestedBy,
                IntegrationTestsFixture.RequestedBy.GetHashedPassword("abc123-DEF456-ghi789")),
            _integrationTestsFixture.CancellationToken);
        await _integrationTestsFixture.CommitAsync();

        ValidatorResponse response = await _authService.ValidateJwtAsync(
            IntegrationTestsFixture.RequestedBy, cancellationToken: _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeFalse();
        response.ErrorCode.Should().BeNull();
        response.ErrorMessage.Should().BeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateJwtShouldReturnClaimNotFoundInRequestHeadersAuthorization(string? request)
    {
        ValidatorResponse response =
            await _authService.ValidateJwtAsync(request, cancellationToken: _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(404);
        response.ErrorMessage.Should().Be("Claim not found in request headers authorization.");
    }

    [Fact]
    public async Task ValidateJwtShouldReturnUnauthorizedAccess()
    {
        ValidatorResponse response = await _authService.ValidateJwtAsync(
            IntegrationTestsFixture.RequestedBy, cancellationToken: _integrationTestsFixture.CancellationToken);

        response.IsFailure.Should().BeTrue();
        response.ErrorCode.Should().Be(401);
        response.ErrorMessage.Should().Be("Unauthorized access.");
    }
}
