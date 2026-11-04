using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.UpdateUser;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.UpdateUser;

public class AvatarPolicyTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public void GetExtensionShouldReturnTheSupportedExtension(string contentType, string expected)
    {
        string extension = AvatarPolicy.GetExtension(contentType);

        Assert.Equal(expected, extension);
    }

    [Fact]
    public void GetExtensionShouldThrowArgumentOutOfRangeExceptionWhenTheExtensionIsUnsupported()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => AvatarPolicy.GetExtension("image/gif"));

        Assert.Equal("contentType", exception.ParamName);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void ValidateShouldReturnNoFailuresWhenAcceptingSupportedImageSignatures(string contentType)
    {
        IReadOnlyList<Failure> failures = AvatarPolicy.Validate(CreateImage(contentType), contentType);

        Assert.Empty(failures);
    }

    [Fact]
    public void ValidateShouldReturnNoFailuresWhenAcceptingTheMaximumImageSize()
    {
        byte[] avatar = new byte[AvatarPolicy.MaxSizeInBytes];
        CreateImage("image/jpeg").CopyTo(avatar, 0);

        IReadOnlyList<Failure> failures = AvatarPolicy.Validate(avatar, "image/jpeg");

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateShouldReturnValidationFailuresWhenTheImageIsMissing(bool missing)
    {
        ReadOnlyMemory<byte>? avatar = null;
        if (!missing)
            avatar = ReadOnlyMemory<byte>.Empty;

        IReadOnlyList<Failure> failures = AvatarPolicy.Validate(avatar, "image/jpeg");

        Assert.Equal("identity.users.avatar.required", Assert.Single(failures).Code);
    }

    [Fact]
    public void ValidateShouldReturnValidationFailuresWhenAnOversizedImageIsProvided()
    {
        IReadOnlyList<Failure> failures = AvatarPolicy.Validate(new byte[AvatarPolicy.MaxSizeInBytes + 1], "image/png");

        Assert.Equal("identity.users.avatar.maximum-size", Assert.Single(failures).Code);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData(null)]
    [InlineData("image/png")]
    public void ValidateShouldReturnValidationFailuresWhenAnUnsupportedOrMismatchedContentTypeIsProvided(string? contentType = null)
    {
        IReadOnlyList<Failure> failures = AvatarPolicy.Validate(CreateImage("image/jpeg"), contentType);

        Assert.Equal("identity.users.avatar.invalid", Assert.Single(failures).Code);
    }

    [Theory]
    [InlineData("image/jpeg", 2)]
    [InlineData("image/png", 7)]
    [InlineData("image/webp", 11)]
    public void ValidateShouldReturnValidationFailuresWhenATruncatedSignatureIsProvided(string contentType, int size)
    {
        byte[] avatar = CreateImage(contentType)[..size];

        IReadOnlyList<Failure> failures = AvatarPolicy.Validate(avatar, contentType);

        Assert.Equal("identity.users.avatar.invalid", Assert.Single(failures).Code);
    }

    [Theory]
    [InlineData("image/jpeg", 0)]
    [InlineData("image/jpeg", 1)]
    [InlineData("image/jpeg", 2)]
    [InlineData("image/png", 0)]
    [InlineData("image/png", 1)]
    [InlineData("image/png", 2)]
    [InlineData("image/png", 3)]
    [InlineData("image/png", 4)]
    [InlineData("image/png", 5)]
    [InlineData("image/png", 6)]
    [InlineData("image/png", 7)]
    [InlineData("image/webp", 0)]
    [InlineData("image/webp", 1)]
    [InlineData("image/webp", 2)]
    [InlineData("image/webp", 3)]
    [InlineData("image/webp", 8)]
    [InlineData("image/webp", 9)]
    [InlineData("image/webp", 10)]
    [InlineData("image/webp", 11)]
    public void ValidateShouldReturnValidationFailuresWhenEachCorruptedImageSignatureAreProvided(string contentType, int byteIndex)
    {
        byte[] avatar = CreateImage(contentType);
        avatar[byteIndex] ^= 0xFF;

        IReadOnlyList<Failure> failures = AvatarPolicy.Validate(avatar, contentType);

        Assert.Equal("identity.users.avatar.invalid", Assert.Single(failures).Code);
    }

    private static byte[] CreateImage(string contentType) => contentType switch { "image/jpeg" => [0xFF, 0xD8, 0xFF], "image/png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], _ => [.. "RIFF0000WEBP"u8] };
}
