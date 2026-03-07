using System.Text.RegularExpressions;

namespace AspireBoot.ApiService.Validators;

public static partial class PasswordValidator
{
    public static ValidatorResponse ValidateFormat(string? password) =>
        string.IsNullOrWhiteSpace(password) || !Regex().IsMatch(password)
            ? ValidatorResponse.Failure(400, "Invalid password format.")
            : ValidatorResponse.Success();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])[a-zA-Z\d\W]{16,32}$")]
    private static partial Regex Regex();
}
