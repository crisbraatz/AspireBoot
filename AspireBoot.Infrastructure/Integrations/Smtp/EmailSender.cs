using System.Net.Sockets;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Observability;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AspireBoot.Infrastructure.Integrations.Smtp;

public class EmailSender(Func<ISmtpClient> func, SmtpOptions smtpOptions) : IEmailSender
{
    public EmailSender(SmtpOptions smtpOptions) : this(() => new SmtpClient(), smtpOptions)
    {
    }

    public async Task<Result> SendAsync(EmailDetails emailDetails, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using MimeMessage mimeMessage = new();

        mimeMessage.From.Add(MailboxAddress.Parse(smtpOptions.From));
        mimeMessage.To.Add(MailboxAddress.Parse(emailDetails.Recipient));
        mimeMessage.Subject = emailDetails.Subject;
        mimeMessage.Body =
            new BodyBuilder { HtmlBody = emailDetails.Html, TextBody = emailDetails.Text }.ToMessageBody();

        using DependencyTrace dependencyTrace = new(DependencySources.Smtp, "mailkit", "send");
        using ISmtpClient smtpClient = func();

        try
        {
            await smtpClient
                .ConnectAsync(
                    smtpOptions.Host,
                    smtpOptions.Port,
                    smtpOptions.StartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(smtpOptions.Username))
                await smtpClient
                    .AuthenticateAsync(smtpOptions.Username, smtpOptions.Password, cancellationToken)
                    .ConfigureAwait(false);

            await smtpClient.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
            await smtpClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            dependencyTrace.Fail(exception.GetType().Name, "MailKit is temporarily unavailable.");

            throw new DependencyUnavailableException(DependencyKind.Smtp, exception);
        }
        catch (SmtpCommandException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "MailKit integration failed.");

            throw;
        }
    }

    private static bool IsUnavailable(Exception exception)
    {
        if (exception is SmtpCommandException smtpCommandException)
            return (int)smtpCommandException.StatusCode is >= 400 and < 500;

        return exception is
            IOException or
            ServiceNotConnectedException or
            SmtpProtocolException or
            SocketException or
            TimeoutException;
    }
}
