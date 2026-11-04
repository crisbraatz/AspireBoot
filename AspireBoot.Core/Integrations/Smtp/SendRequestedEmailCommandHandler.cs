using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Integrations.Smtp;

public class SendRequestedEmailCommandHandler(
    IEnumerable<IEmailPreparation> emailPreparations,
    IEmailRequestLimiter emailRequestLimiter,
    IEmailSender emailSender,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<SendRequestedEmailCommand>
{
    private readonly Dictionary<EmailRequestKind, IEmailPreparation> _emailPreparations =
        emailPreparations.ToDictionary(emailPreparation => emailPreparation.EmailRequestKind);

    public async Task<Result> HandleAsync(SendRequestedEmailCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.EmailRequestedEvent.MessageId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.EmailRequestedEvent.IdentityUserId) ||
            string.IsNullOrWhiteSpace(command.EmailRequestedEvent.Email) ||
            !new EmailAddressAttribute().IsValid(command.EmailRequestedEvent.Email) ||
            !Enum.IsDefined(command.EmailRequestedEvent.EmailRequestKind))
            return Result.Failure(
            [
                Failure.Validation(
                    "integrations.smtp.invalid-email-request",
                    "Email request is invalid.",
                    string.Empty)
            ]);

        if (!_emailPreparations.TryGetValue(
                command.EmailRequestedEvent.EmailRequestKind, out IEmailPreparation? emailPreparation))
            throw new InvalidOperationException(
                $"No email preparation registered for {command.EmailRequestedEvent.EmailRequestKind}.");

        if (command.EmailRequestedEvent.GetType() != emailPreparation.EventType)
            return Result.Failure(
            [
                Failure.Validation(
                    "integrations.smtp.invalid-event-type",
                    "Event type does not match its email kind.",
                    string.Empty)
            ]);

        Result validationResult = emailPreparation.Validate(command.EmailRequestedEvent);

        if (!validationResult.IsSuccess)
            return validationResult;

        IdentityUser? identityUser =
            await userManager.FindByIdAsync(command.EmailRequestedEvent.IdentityUserId).ConfigureAwait(false);

        if (identityUser is null ||
            !string.Equals(identityUser.Email, command.EmailRequestedEvent.Email, StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        User? user = await userRepository
            .FindByIdentityUserIdAsync(identityUser.Id, true, true, cancellationToken).ConfigureAwait(false);

        if (user is null || !emailPreparation.CanSend(identityUser, user))
            return Result.Success();

        if (emailPreparation.ApplyRequestLimit)
        {
            Result<bool> canSendResult = await emailRequestLimiter
                .CanSendAsync(
                    identityUser.Id,
                    command.EmailRequestedEvent.Recipient,
                    command.EmailRequestedEvent.MessageId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!canSendResult.IsSuccess)
                return Result.Failure(canSendResult.Failures!);

            if (!canSendResult.Value)
                return Result.Failure(
                [
                    Failure.TooManyRequests(
                        "email.rate-limit.exceeded",
                        "Too many email requests were sent. Try again later.")
                ]);
        }

        Result<EmailDetails> prepareResult = await emailPreparation
            .PrepareAsync(identityUser, user, command.EmailRequestedEvent, cancellationToken).ConfigureAwait(false);

        if (!prepareResult.IsSuccess)
            return Result.Failure(prepareResult.Failures!);

        cancellationToken.ThrowIfCancellationRequested();

        return await emailSender.SendAsync(prepareResult.Value!, cancellationToken).ConfigureAwait(false);
    }
}
