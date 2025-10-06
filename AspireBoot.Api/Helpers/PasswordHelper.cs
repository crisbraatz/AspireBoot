using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Api.Helpers;

public static class PasswordHelper
{
    private static readonly PasswordHasher<string> PasswordHasher = new();

    public static string GetHashedPassword(this string user, string password) =>
        PasswordHasher.HashPassword(user, password);

    public static bool IsMatch(this string user, string hashedPassword, string providedPassword) =>
        PasswordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword)
            is not PasswordVerificationResult.Failed;
}