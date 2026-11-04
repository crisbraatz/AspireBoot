using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;

namespace AspireBoot.Core.Identity.Auth.Register;

public class RegisterCommandValidator : ICommandValidator<RegisterCommand>
{
    public IReadOnlyList<Failure> Validate(RegisterCommand command)
    {
        if (IdentityConstants.NormalizeName(command.Name).Length is < 2 or > 100)
            return
            [
                Failure.Validation(
                    "identity.auth.name.length",
                    "Name must contain between 2 and 100 characters.",
                    "name")
            ];

        if (!NamePolicy.Validate(command.Name))
            return
            [
                Failure.Validation(
                    "identity.auth.name.characters",
                    "Name must contain only letters and spaces.",
                    "name")
            ];

        return [];
    }
}
