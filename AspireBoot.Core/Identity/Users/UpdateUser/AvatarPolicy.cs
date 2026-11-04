using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.UpdateUser;

public static class AvatarPolicy
{
    public const int MaxSizeInBytes = 4 * 1024 * 1024;

    public static string GetExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType))
    };

    public static IReadOnlyList<Failure> Validate(ReadOnlyMemory<byte>? content = null, string? contentType = null)
    {
        if (content is not { } image || image.IsEmpty)
            return
            [
                Failure.Validation(
                    "identity.users.avatar.required",
                    "Avatar image is required.",
                    "avatar")
            ];

        if (image.Length > MaxSizeInBytes)
            return
            [
                Failure.Validation(
                    "identity.users.avatar.maximum-size",
                    "Avatar image must not exceed 4 MB.",
                    "avatar")
            ];

        return contentType switch
        {
            "image/jpeg" => image.Span.Length >= 3 &&
                            image.Span[0] is 0xFF &&
                            image.Span[1] is 0xD8 &&
                            image.Span[2] is 0xFF,
            "image/png" => image.Span.Length >= 8 &&
                           image.Span[0] is 0x89 &&
                           image.Span[1] is 0x50 &&
                           image.Span[2] is 0x4E &&
                           image.Span[3] is 0x47 &&
                           image.Span[4] is 0x0D &&
                           image.Span[5] is 0x0A &&
                           image.Span[6] is 0x1A &&
                           image.Span[7] is 0x0A,
            "image/webp" => image.Span.Length >= 12 &&
                            image.Span[..4].SequenceEqual("RIFF"u8) &&
                            image.Span[8..12].SequenceEqual("WEBP"u8),
            _ => false
        }
            ? []
            :
            [
                Failure.Validation(
                    "identity.users.avatar.invalid",
                    "Avatar must be a valid JPEG, PNG or WebP image.",
                    "avatar")
            ];
    }
}
