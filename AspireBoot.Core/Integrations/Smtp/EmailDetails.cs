namespace AspireBoot.Core.Integrations.Smtp;

public record EmailDetails(string Recipient, string Subject, string Html, string Text);
