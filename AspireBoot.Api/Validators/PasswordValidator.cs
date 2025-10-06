using System.Text.RegularExpressions;

namespace AspireBoot.Api.Validators;

public static partial class PasswordValidator
{
    public static ValidatorResponse ValidateFormat(string? password) =>
        string.IsNullOrWhiteSpace(password) || !Regex().IsMatch(password)
            ? ValidatorResponse.Failure(400, "Invalid password format.")
            : ValidatorResponse.Success();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,16}$")]
    private static partial Regex Regex();
}