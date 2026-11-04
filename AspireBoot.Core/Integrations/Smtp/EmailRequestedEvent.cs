using AspireBoot.Core.Integrations.Messaging;

namespace AspireBoot.Core.Integrations.Smtp;

public abstract record EmailRequestedEvent(string IdentityUserId, string Email, EmailRequestKind EmailRequestKind)
    : AbstractEvent
{
    public virtual string Recipient => Email;
}
