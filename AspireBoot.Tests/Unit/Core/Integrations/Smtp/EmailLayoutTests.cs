using System.Text.Encodings.Web;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Tests.Unit.Core.Integrations.Smtp;

public class EmailLayoutTests
{
    [Fact]
    public void CreateShouldReturnEmailDetailsWhenRenderingTheEmbeddedLayoutAndPreservingPlainText()
    {
        EmailDetails emailDetails = EmailLayout.Create("user@domain.com", "Subject", "Title", "Category", EmailLayout.WithParagraph("Body"), "Plain text");

        Assert.Equal("user@domain.com", emailDetails.Recipient);
        Assert.Equal("Subject", emailDetails.Subject);
        Assert.Equal("Plain text", emailDetails.Text);
        Assert.Contains("<title>AspireBoot</title>", emailDetails.Html, StringComparison.Ordinal);
        Assert.Contains("<svg", emailDetails.Html, StringComparison.Ordinal);
        Assert.Contains("Body", emailDetails.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", emailDetails.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateShouldReturnEmailDetailsWhenEncodingDynamicTextWithoutEncodingTheComposedBody()
    {
        const string untrusted = "<img src=x onerror=alert(1)>";

        EmailDetails emailDetails = EmailLayout.Create("user@domain.com", untrusted, untrusted, untrusted, EmailLayout.WithParagraph(untrusted) + EmailLayout.WithNote(untrusted) + EmailLayout.WithDetail(untrusted, untrusted), "Text");

        Assert.DoesNotContain(untrusted, emailDetails.Html, StringComparison.Ordinal);
        Assert.Contains(HtmlEncoder.Default.Encode(untrusted), emailDetails.Html, StringComparison.Ordinal);
        Assert.Contains("<p", emailDetails.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateWithLinkShouldReturnEmailDetailsWhenRenderingAnEncodedActionLinkAndItsPlainTextAlternative()
    {
        const string link = "https://app.tests/confirm?token=abc&email=user%40domain.com";

        EmailDetails emailDetails = EmailLayout.CreateWithLink("user@domain.com", "Subject", "Title", "Category", "Description", "Confirm", link, "Note");

        Assert.Contains(HtmlEncoder.Default.Encode(link), emailDetails.Html, StringComparison.Ordinal);
        Assert.Contains(link, emailDetails.Text, StringComparison.Ordinal);
        Assert.Contains("Description", emailDetails.Text, StringComparison.Ordinal);
        Assert.Contains("Note", emailDetails.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateWithLinkShouldReturnEmailDetailsWhenEncodingLinkAttributesAndAllUserControlledLinkText()
    {
        const string unsafeText = "<script>alert(1)</script>";
        const string link = "https://app.tests/confirm?token=\"value\"&email=user@example.com";

        EmailDetails emailDetails = EmailLayout.CreateWithLink("user@example.com", unsafeText, unsafeText, unsafeText, unsafeText, unsafeText, link, unsafeText);

        Assert.DoesNotContain(unsafeText, emailDetails.Html, StringComparison.Ordinal);
        Assert.Contains(HtmlEncoder.Default.Encode(unsafeText), emailDetails.Html, StringComparison.Ordinal);
        Assert.Contains("href=\"" + HtmlEncoder.Default.Encode(link) + "\"", emailDetails.Html, StringComparison.Ordinal);
        Assert.Equal($"{unsafeText}\n\n{unsafeText}: {link}\n\n{unsafeText}", emailDetails.Text);
    }

    [Fact]
    public void FormatDateTimeShouldReturnTheTimestampFormattedInUtc()
    {
        string formatted = EmailLayout.FormatDateTime(new DateTimeOffset(2026, 10, 20, 6, 0, 0, TimeSpan.FromHours(3)));

        Assert.Equal("2026-10-20 03:00 UTC", formatted);
    }
}
