using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Integrations.Smtp;

public interface IEmailPreparation
{
    bool ApplyRequestLimit { get; }
    EmailRequestKind EmailRequestKind { get; }
    Type EventType { get; }

    bool CanSend(IdentityUser identityUser, User user);

    Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser,
        User user,
        EmailRequestedEvent emailRequestedEvent,
        CancellationToken cancellationToken);

    Result Validate(EmailRequestedEvent emailRequestedEvent);
}
