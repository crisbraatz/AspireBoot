using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Integrations.Smtp;

public abstract class EmailPreparation<TEvent> : IEmailPreparation where TEvent : EmailRequestedEvent
{
    public virtual bool ApplyRequestLimit => false;
    public abstract EmailRequestKind EmailRequestKind { get; }
    public Type EventType => typeof(TEvent);

    protected virtual bool CanSend(IdentityUser identityUser, User user) => user.IsActive;

    protected abstract Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser, User user, TEvent emailRequestedEvent, CancellationToken cancellationToken);

    protected virtual Result Validate(TEvent emailRequestedEvent) => Result.Success();

    bool IEmailPreparation.CanSend(IdentityUser identityUser, User user) => CanSend(identityUser, user);

    Task<Result<EmailDetails>> IEmailPreparation.PrepareAsync(
        IdentityUser identityUser,
        User user,
        EmailRequestedEvent emailRequestedEvent,
        CancellationToken cancellationToken) =>
        PrepareAsync(identityUser, user, (TEvent)emailRequestedEvent, cancellationToken);

    Result IEmailPreparation.Validate(EmailRequestedEvent emailRequestedEvent) => Validate((TEvent)emailRequestedEvent);
}
