using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Integrations.Smtp;

public class SendRequestedEmailCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessAndSendPreparedMail()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        EmailDetails emailDetails = new("user@example.com", "Subject", "<p>Body</p>", "Body");

        Result result = await new SendRequestedEmailCommandHandler(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, emailDetails)], userManager: userManager, userRepository: userRepository).HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailSender.Received(1).SendAsync(emailDetails, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessAndSendUsingTheDefaultPreparationRules()
    {
        using UserManager<IdentityUser> userManager = MockUserManager(new IdentityUser { Id = "account", Email = "user@example.com", EmailConfirmed = true });
        IUserRepository userRepository = MockUserRepository(new User("account", "user@example.com", "User Name"));
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();

        Result result = await new SendRequestedEmailCommandHandler(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [new AspireBoot.Core.Identity.Auth.ConfirmEmail.ConfirmEmailPreparation()], userManager: userManager, userRepository: userRepository).HandleAsync(new SendRequestedEmailCommand(new AspireBoot.Core.Identity.Auth.ConfirmEmail.ConfirmEmailEvent("account", "user@example.com")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailSender.Received(1).SendAsync(Arg.Is<EmailDetails>(emailDetails => emailDetails.Subject == "Welcome to AspireBoot"), Arg.Any<CancellationToken>());
        await emailRequestLimiter.DidNotReceive().CanSendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenUsingTheEventIdForIdempotencyAndMatchingTheAccountEmailIgnoringCase()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "USER@EXAMPLE.COM" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        EmailDetails emailDetails = new("user@example.com", "Subject", "<p>Body</p>", "Body");
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");

        Result result = await new SendRequestedEmailCommandHandler(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, emailDetails)], userManager: userManager, userRepository: userRepository).HandleAsync(new SendRequestedEmailCommand(sendEmailConfirmationEvent), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailRequestLimiter.Received(1).CanSendAsync(identityUser.Id, sendEmailConfirmationEvent.Recipient, sendEmailConfirmationEvent.MessageId, TestContext.Current.CancellationToken);
        await emailSender.Received(1).SendAsync(emailDetails, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenSkippingAnEventForAnOutdatedAddress()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        identityUser.Email = "changed@example.com";

        Result result = await new SendRequestedEmailCommandHandler(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"))], userManager: userManager, userRepository: userRepository).HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenSkippingAnIneligibleAccount()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEmailPreparation emailPreparation = MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"));
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [emailPreparation], userManager: userManager, userRepository: userRepository);
        emailPreparation.CanSend(identityUser, user).Returns(false);

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenNoQuotaApplies()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        EmailDetails emailDetails = new("user@example.com", "Subject", "<p>Body</p>", "Body");
        IEmailPreparation emailPreparation = MockEmailPreparation(identityUser, user, emailDetails);
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [emailPreparation], userManager: userManager, userRepository: userRepository);
        emailPreparation.ApplyRequestLimit.Returns(false);

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailSender.Received(1).SendAsync(emailDetails, Arg.Any<CancellationToken>());
        await emailRequestLimiter.DidNotReceive().CanSendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("message-id")]
    [InlineData("identity-user-id")]
    [InlineData("missing-email")]
    [InlineData("invalid-email")]
    [InlineData("email-kind")]
    public async Task ShouldReturnFailureWhenTheEventDataIsInvalid(string invalidField)
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");
        sendEmailConfirmationEvent = invalidField switch { "message-id" => sendEmailConfirmationEvent with { MessageId = Guid.Empty }, "identity-user-id" => sendEmailConfirmationEvent with { IdentityUserId = " " }, "missing-email" => sendEmailConfirmationEvent with { Email = " " }, "invalid-email" => sendEmailConfirmationEvent with { Email = "invalid" }, _ => sendEmailConfirmationEvent with { EmailRequestKind = (EmailRequestKind)int.MaxValue } };

        Result result = await new SendRequestedEmailCommandHandler(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"))], userManager: userManager, userRepository: userRepository).HandleAsync(new SendRequestedEmailCommand(sendEmailConfirmationEvent), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenNoPreparationMatchesTheEmailKind()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEmailPreparation emailPreparation = MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"));
        emailPreparation.EmailRequestKind.Returns(EmailRequestKind.RequestRecovery);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new SendRequestedEmailCommandHandler(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [emailPreparation], userManager: userManager, userRepository: userRepository).HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken));

        Assert.Contains(nameof(EmailRequestKind.SendEmailConfirmation), exception.Message, StringComparison.Ordinal);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenAnEventThatDoesNotMatchItsRegisteredTypeIsProvided()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEmailPreparation emailPreparation = MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"));
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [emailPreparation], userManager: userManager, userRepository: userRepository);
        emailPreparation.EventType.Returns(typeof(AspireBoot.Core.Identity.Auth.RequestRecovery.RequestRecoveryEvent));

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.Equal("integrations.smtp.invalid-event-type", Assert.Single(result.Failures!).Code);
        await userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenPreservingEventPreparationValidationFailures()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEmailPreparation emailPreparation = MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"));
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [emailPreparation], userManager: userManager, userRepository: userRepository);
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");
        Result expectedResult = Result.Failure([Failure.Validation("email.recipient-invalid", "Invalid.", "recipient")]);
        emailPreparation.Validate(sendEmailConfirmationEvent).Returns(expectedResult);

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(sendEmailConfirmationEvent), TestContext.Current.CancellationToken);

        Assert.Same(expectedResult, result);
        await userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenSkipAnUnknownAccount()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"))], userManager: userManager, userRepository: userRepository);
        userManager.FindByIdAsync("account").Returns(Task.FromResult<IdentityUser?>(null));

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenSkipAMissingUser()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"))], userManager: userManager, userRepository: userRepository);
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingEmailLimiterFailures()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEmailPreparation emailPreparation = MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"));
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [emailPreparation], userManager: userManager, userRepository: userRepository);
        Failure failure = Failure.ServiceUnavailable("email.limiter-unavailable", "Unavailable.");
        emailRequestLimiter.CanSendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<bool>.Failure([failure])));

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.Equal(failure, Assert.Single(result.Failures!));
        await emailPreparation.DidNotReceive().PrepareAsync(Arg.Any<IdentityUser>(), Arg.Any<User>(), Arg.Any<EmailRequestedEvent>(), Arg.Any<CancellationToken>());
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsFailureWhenRespectTheEmailQuota()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"))], userManager: userManager, userRepository: userRepository);
        emailRequestLimiter.CanSendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<bool>.Success(false)));

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.TooManyRequests, Assert.Single(result.Failures!).FailureKind);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingPreparationFailures()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEmailPreparation emailPreparation = MockEmailPreparation(identityUser, user, new EmailDetails("user@example.com", "Subject", "<p>Body</p>", "Body"));
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [emailPreparation], userManager: userManager, userRepository: userRepository);
        emailPreparation.PrepareAsync(identityUser, user, Arg.Any<EmailRequestedEvent>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<EmailDetails>.Failure([Failure.ServiceUnavailable("preparation.unavailable", "Unavailable.")])));

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.Equal("preparation.unavailable", Assert.Single(result.Failures!).Code);
        await emailSender.DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingDeliveryFailures()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com" };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = MockUserRepository(user);
        IEmailSender emailSender = MockEmailSender();
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        EmailDetails emailDetails = new("user@example.com", "Subject", "<p>Body</p>", "Body");
        SendRequestedEmailCommandHandler commandHandler = new(emailRequestLimiter: emailRequestLimiter, emailSender: emailSender, emailPreparations: [MockEmailPreparation(identityUser, user, emailDetails)], userManager: userManager, userRepository: userRepository);
        emailSender.SendAsync(emailDetails, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])));

        Result result = await commandHandler.HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.Equal("smtp.unavailable", Assert.Single(result.Failures!).Code);
    }

    private static IEmailPreparation MockEmailPreparation(IdentityUser identityUser, User user, EmailDetails emailDetails)
    {
        IEmailPreparation emailPreparation = Substitute.For<IEmailPreparation>();
        emailPreparation.EmailRequestKind.Returns(EmailRequestKind.SendEmailConfirmation);
        emailPreparation.EventType.Returns(typeof(SendEmailConfirmationEvent));
        emailPreparation.ApplyRequestLimit.Returns(true);
        emailPreparation.Validate(Arg.Any<EmailRequestedEvent>()).Returns(Result.Success());
        emailPreparation.CanSend(identityUser, user).Returns(true);
        emailPreparation.PrepareAsync(identityUser, user, Arg.Any<EmailRequestedEvent>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<EmailDetails>.Success(emailDetails)));
        return emailPreparation;
    }

    private static IEmailRequestLimiter MockEmailRequestLimiter()
    {
        IEmailRequestLimiter emailRequestLimiter = Substitute.For<IEmailRequestLimiter>();
        emailRequestLimiter.CanSendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<bool>.Success(true)));
        return emailRequestLimiter;
    }

    private static IEmailSender MockEmailSender()
    {
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        emailSender.SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        return emailSender;
    }

    private static UserManager<IdentityUser> MockUserManager(IdentityUser? identityUser = null)
    {
        UserManager<IdentityUser> userManager = Substitute.For<UserManager<IdentityUser>>(Substitute.For<IUserStore<IdentityUser>>(), Options.Create(new IdentityOptions()), Substitute.For<IPasswordHasher<IdentityUser>>(), Array.Empty<IUserValidator<IdentityUser>>(), Array.Empty<IPasswordValidator<IdentityUser>>(), Substitute.For<ILookupNormalizer>(), new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(), NullLogger<UserManager<IdentityUser>>.Instance);
        userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(Task.FromResult(identityUser));
        return userManager;
    }

    private static IUserRepository MockUserRepository(User user)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        return userRepository;
    }
}
