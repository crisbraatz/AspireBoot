using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Integrations.Smtp;

public interface IEmailSender
{
    Task<Result> SendAsync(EmailDetails emailDetails, CancellationToken cancellationToken);
}
