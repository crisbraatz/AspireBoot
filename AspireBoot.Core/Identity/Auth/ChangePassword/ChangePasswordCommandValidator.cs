using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.ChangePassword;

public class ChangePasswordCommandValidator : ICommandValidator<ChangePasswordCommand>
{
    public IReadOnlyList<Failure> Validate(ChangePasswordCommand command)
    {
        if (string.Equals(command.CurrentPassword, command.NewPassword, StringComparison.Ordinal))
            return
            [
                Failure.Validation(
                    "identity.auth.password-unchanged",
                    "The new password must be different from the current password.",
                    "newPassword")
            ];

        return [];
    }
}
