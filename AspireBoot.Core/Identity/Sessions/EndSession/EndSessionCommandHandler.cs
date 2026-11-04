using AspireBoot.Core.Cqrs;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Sessions.EndSession;

public class EndSessionCommandHandler(SignInManager<IdentityUser> signInManager) : ICommandHandler<EndSessionCommand>
{
    public async Task<Result> HandleAsync(EndSessionCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await signInManager.SignOutAsync().ConfigureAwait(false);

        return Result.Success();
    }
}
