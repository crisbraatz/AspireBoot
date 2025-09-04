using System.Text.RegularExpressions;

namespace AspireBoot.Api.Validators;

public static partial class EmailValidator
{
    public static ValidatorResponse ValidateFormat(string email) => !Regex().IsMatch(email)
        ? ValidatorResponse.Failure(400, "Invalid email format.")
        : ValidatorResponse.Success();

    [GeneratedRegex(@"^[\w!#$%&'*+/=?`{|}~^-]+(?:\.[\w!#$%&'*+/=?`{|}~^-]+)*@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}$")]
    private static partial Regex Regex();
}