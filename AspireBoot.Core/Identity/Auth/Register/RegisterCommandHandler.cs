using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.Register;

public class RegisterCommandHandler(IUserRepository userRepository) : ICommandHandler<RegisterCommand, RegisterResult>
{
    public async Task<Result<RegisterResult>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        string email = IdentityConstants.NormalizeEmail(command.Email);
        string identityUserId = Guid.CreateVersion7().ToString();
        IdentityUser identityUser = new() { Id = identityUserId, Email = email, UserName = identityUserId };
        bool requiresEmailConfirmation = !CoreConstants.IsDevelopment;

        IdentityResult result = await userRepository
            .TryAddAsync(
                new User(identityUser.Id, email, IdentityConstants.NormalizeName(command.Name)),
                identityUser,
                command.Password,
                requiresEmailConfirmation
                    ? new SendEmailConfirmationEvent(identityUser.Id, email, command.FrontendOrigin)
                    : null,
                cancellationToken)
            .ConfigureAwait(false);

        IdentityError[] identityErrors =
            [.. result.Errors.Where(identityError => identityError.Code is not "DuplicateEmail")];

        if (!result.Succeeded && identityErrors.Length > 0)
            return Result<RegisterResult>.Failure(identityErrors
                .Select(identityError =>
                    Failure.Validation(
                        "identity.auth." + identityError.Code,
                        identityError.Code,
                        identityError.Code switch
                        {
                            "InvalidEmail" => "email",
                            _ when identityError.Code.StartsWith("Password", StringComparison.Ordinal) => "password",
                            _ => string.Empty
                        }))
                .ToArray());

        return Result<RegisterResult>.Success(new RegisterResult(requiresEmailConfirmation));
    }
}
