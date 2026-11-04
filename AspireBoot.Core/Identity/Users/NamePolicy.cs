using System.Text.RegularExpressions;

namespace AspireBoot.Core.Identity.Users;

public static partial class NamePolicy
{
    public const string AllowedPattern = @"\A[\p{L}\p{M}\s]+\z";

    public static bool Validate(string name) => NamePattern().IsMatch(IdentityConstants.NormalizeName(name));

    [GeneratedRegex(@"\A\p{L}[\p{L}\p{M}]*(?: \p{L}[\p{L}\p{M}]*)*\z", RegexOptions.None, 100)]
    private static partial Regex NamePattern();
}
