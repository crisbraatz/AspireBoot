using System.Net.Sockets;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Integrations.Smtp;
using AspireBoot.Infrastructure.Observability;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Infrastructure.Integrations.Smtp;

public class EmailSenderTests
{
    [Theory]
    [InlineData("user", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public async Task ShouldReturnSuccessWhenConfigured(string username, bool startTls)
    {
        SmtpOptions smtpOptions = CreateSmtpOptions(username, startTls);
        ISmtpClient smtpClient = MockSmtpClient();
        List<EmailDetails> sentEmails = [];
        List<string> senders = [];
        smtpClient.SendAsync(Arg.Do<MimeMessage>(mimeMessage =>
        {
            sentEmails.Add(new EmailDetails(mimeMessage.To.Mailboxes.Single().Address, mimeMessage.Subject!, mimeMessage.HtmlBody!, mimeMessage.TextBody!));
            senders.Add(mimeMessage.From.Mailboxes.Single().Address);
        }), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress?>()).Returns(Task.FromResult("Sent."));
        EmailDetails emailDetails = new("recipient@example.com", "Confirm email", "<p>Confirm</p>", "Confirm");

        Result result = await new EmailSender(smtpOptions: smtpOptions, func: () => smtpClient).SendAsync(emailDetails, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(emailDetails, Assert.Single(sentEmails));
        Assert.Equal(smtpOptions.From, Assert.Single(senders));
        await smtpClient.Received(1).ConnectAsync(smtpOptions.Host, smtpOptions.Port, startTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, TestContext.Current.CancellationToken);
        if (string.IsNullOrWhiteSpace(username))
            await smtpClient.DidNotReceive().AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        else
            await smtpClient.Received(1).AuthenticateAsync(username, smtpOptions.Password, TestContext.Current.CancellationToken);
        await smtpClient.Received(1).DisconnectAsync(true, TestContext.Current.CancellationToken);
        smtpClient.Received(1).Dispose();
    }

    [Theory]
    [InlineData("io")]
    [InlineData("socket")]
    [InlineData("timeout")]
    [InlineData("disconnected")]
    [InlineData("protocol")]
    [InlineData("temporary-smtp")]
    public async Task ShouldThrowDependencyUnavailableExceptionWhenWrapTemporarySmtpFailures(string scenario)
    {
        ISmtpClient smtpClient = MockSmtpClient();
        Exception exception = CreateException(scenario);
        smtpClient.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress?>()).Returns(Task.FromException<string>(exception));

        DependencyUnavailableException dependencyUnavailableException = await Assert.ThrowsAsync<DependencyUnavailableException>(() => new EmailSender(smtpOptions: CreateSmtpOptions(), func: () => smtpClient).SendAsync(new EmailDetails("recipient@example.com", "Subject", "<p>Body</p>", "Body"), TestContext.Current.CancellationToken));

        Assert.Same(exception, dependencyUnavailableException.InnerException);
        Assert.Equal(DependencyKind.Smtp, dependencyUnavailableException.DependencyKind);
        smtpClient.Received(1).Dispose();
    }

    [Fact]
    public async Task ShouldThrowSmtpCommandExceptionWhenSmtpRejectsTheCommandPermanently()
    {
        ISmtpClient smtpClient = MockSmtpClient();
        SmtpCommandException smtpCommandException = new(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "Rejected.");
        smtpClient.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress?>()).Returns(Task.FromException<string>(smtpCommandException));

        SmtpCommandException exceptionSmtpCommandException = await Assert.ThrowsAsync<SmtpCommandException>(() => new EmailSender(smtpOptions: CreateSmtpOptions(), func: () => smtpClient).SendAsync(new EmailDetails("recipient@example.com", "Subject", "<p>Body</p>", "Body"), TestContext.Current.CancellationToken));

        Assert.Same(smtpCommandException, exceptionSmtpCommandException);
        smtpClient.Received(1).Dispose();
    }

    [Fact]
    public async Task ShouldThrowAuthenticationExceptionWhenSmtpAuthenticationFails()
    {
        ISmtpClient smtpClient = MockSmtpClient();
        AuthenticationException authenticationException = new("Rejected credentials.");
        smtpClient.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(authenticationException));

        AuthenticationException exceptionAuthenticationException = await Assert.ThrowsAsync<AuthenticationException>(() => new EmailSender(smtpOptions: CreateSmtpOptions(), func: () => smtpClient).SendAsync(new EmailDetails("recipient@example.com", "Subject", "<p>Body</p>", "Body"), TestContext.Current.CancellationToken));

        Assert.Same(authenticationException, exceptionAuthenticationException);
        await smtpClient.DidNotReceive().SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress?>());
    }

    [Fact]
    public async Task ShouldThrowOperationCanceledExceptionWhenDeliveryIsCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        ISmtpClient smtpClient = MockSmtpClient();
        smtpClient.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress?>()).Returns(async _ =>
        {
            await cancellationTokenSource.CancelAsync();
            return await Task.FromCanceled<string>(cancellationTokenSource.Token);
        });

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EmailSender(smtpOptions: CreateSmtpOptions(), func: () => smtpClient).SendAsync(new EmailDetails("recipient@example.com", "Subject", "<p>Body</p>", "Body"), cancellationTokenSource.Token));

        Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
        smtpClient.Received(1).Dispose();
    }

    private static Exception CreateException(string scenario) => scenario switch { "io" => new IOException(), "socket" => new SocketException(), "timeout" => new TimeoutException(), "disconnected" => new ServiceNotConnectedException(), "protocol" => new SmtpProtocolException(), _ => new SmtpCommandException(SmtpErrorCode.MessageNotAccepted, SmtpStatusCode.ServiceNotAvailable, "Unavailable.") };

    private static SmtpOptions CreateSmtpOptions(string username = "user", bool startTls = true) => new() { From = "sender@example.com", Host = "smtp.tests", Password = "secret", Port = 587, StartTls = startTls, Username = username };

    private static ISmtpClient MockSmtpClient()
    {
        ISmtpClient smtpClient = Substitute.For<ISmtpClient>();
        smtpClient.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress?>()).Returns(Task.FromResult("Sent."));
        return smtpClient;
    }
}
