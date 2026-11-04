using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.UpdateUser;

public class UpdateUserCommandValidator : ICommandValidator<UpdateUserCommand>
{
    public IReadOnlyList<Failure> Validate(UpdateUserCommand command)
    {
        List<Failure> failures = [];

        if (command.NewName is null &&
            (command.Avatar is null || command.AvatarContentType is null))
            failures.Add(Failure.Validation(
                "identity.users.empty",
                "At least one user field must be provided.",
                "none"));

        if (command.NewName is not null)
        {
            string newName = IdentityConstants.NormalizeName(command.NewName);

            if (newName.Length is < 2 or > 100)
                failures.Add(Failure.Validation(
                    "identity.users.name.length",
                    "Name must contain between 2 and 100 characters.",
                    "newName"));
            else if (!NamePolicy.Validate(newName))
                failures.Add(Failure.Validation(
                    "identity.users.name.characters",
                    "Name must contain only letters and spaces.",
                    "newName"));
        }

        if (command.Avatar is not null)
            failures.AddRange(AvatarPolicy.Validate(command.Avatar, command.AvatarContentType));

        return failures;
    }
}
