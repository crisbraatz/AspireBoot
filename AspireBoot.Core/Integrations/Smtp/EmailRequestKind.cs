namespace AspireBoot.Core.Integrations.Smtp;

public enum EmailRequestKind
{
    ChangeEmail,
    ConfirmEmail,
    CreateSession,
    DeleteUser,
    PasswordChanged,
    RequestRecovery,
    SendEmailConfirmation
}
