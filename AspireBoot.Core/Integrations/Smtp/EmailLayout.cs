using System.Globalization;
using System.Text.Encodings.Web;

namespace AspireBoot.Core.Integrations.Smtp;

public static class EmailLayout
{
    private static readonly Lazy<string> s_emailLayout = new(() =>
    {
        using Stream stream = typeof(EmailLayout).Assembly.GetManifestResourceStream(
                                  "AspireBoot.Core.Integrations.Smtp.EmailLayout.html") ??
                              throw new InvalidOperationException("The shared email layout resource is missing.");
        using StreamReader streamReader = new(stream);

        return streamReader.ReadToEnd();
    });

    public static EmailDetails Create(
        string recipient, string subject, string title, string category, string body, string text) =>
        new(
            recipient,
            subject,
            s_emailLayout.Value
                .Replace("{{Title}}", Encode(title), StringComparison.Ordinal)
                .Replace("{{Subject}}", Encode(subject), StringComparison.Ordinal)
                .Replace("{{Category}}", Encode(category), StringComparison.Ordinal)
                .Replace("{{Body}}", body, StringComparison.Ordinal)
                .Replace(
                    "{{Year}}",
                    DateTimeOffset.UtcNow.Year.ToString(CultureInfo.InvariantCulture),
                    StringComparison.Ordinal),
            text);

    public static EmailDetails CreateWithLink(
        string recipient,
        string subject,
        string title,
        string category,
        string description,
        string label,
        string link,
        string note)
    {
        string safeLink = Encode(link);

        return Create(
            recipient,
            subject,
            title,
            category,
            WithParagraph(description) +
            $"<table role=\"presentation\" style=\"margin:28px 0;\"><tr><td bgcolor=\"#7045af\" style=\"border-radius:12px;background-color:#7045af;\"><a href=\"{safeLink}\" style=\"display:inline-block;padding:16px 24px;color:#ffffff;font-size:14px;font-weight:600;text-decoration:none;\">{Encode(label)} &rarr;</a></td></tr></table>" +
            WithNote(note) +
            $"<p style=\"margin:24px 0 0;color:#b9b1c7;font-size:12px;line-height:20px;\">Button not working? Copy this link into your browser:<br/><a href=\"{safeLink}\" style=\"color:#c5a8f3;word-break:break-all;\">{safeLink}</a></p>",
            $"{description}\n\n{label}: {link}\n\n{note}");
    }

    public static string FormatDateTime(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    public static string WithDetail(string label, string value) =>
        $"<p style=\"margin:0;padding:12px 0;border-bottom:1px solid #37313f;font-size:14px;line-height:22px;\"><span style=\"color:#b9b1c7;\">{Encode(label)}</span><br/><strong style=\"color:#f5f2fa;font-weight:500;word-break:break-word;\">{Encode(value)}</strong></p>";

    public static string WithNote(string value) =>
        $"<p style=\"margin:24px 0 0;padding:18px 20px;background-color:#25212e;border-left:3px solid #c5a8f3;border-radius:0 10px 10px 0;color:#b9b1c7;font-size:13px;line-height:22px;\">{Encode(value)}</p>";

    public static string WithParagraph(string value) =>
        $"<p style=\"margin:0 0 18px;color:#b9b1c7;font-size:16px;line-height:27px;\">{Encode(value)}</p>";

    private static string Encode(string text) => HtmlEncoder.Default.Encode(text);
}
