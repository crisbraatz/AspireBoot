using Microsoft.AspNetCore.Identity;

namespace AspireBoot.ApiService.Helpers;

public static class PasswordHelper
{
    private static readonly PasswordHasher<string> s_passwordHasher = new();

    public static string GetHashedPassword(this string user, string password) =>
        s_passwordHasher.HashPassword(user, password);

    public static bool IsMatch(this string user, string hashedPassword, string providedPassword) =>
        s_passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword)
            is not PasswordVerificationResult.Failed;
}
