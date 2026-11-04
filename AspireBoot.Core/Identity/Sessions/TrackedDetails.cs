using System.Net;
using System.Text.RegularExpressions;

namespace AspireBoot.Core.Identity.Sessions;

public partial record TrackedDetails(
    string Browser,
    string OperatingSystem,
    string DeviceType,
    string? RemoteIpAddress = null)
{
    public static TrackedDetails Create(string userAgent, string? remoteIpAddress = null)
    {
        userAgent = userAgent is { Length: <= 1024 } ? userAgent : string.Empty;
        Match match = BrowserPattern().Match(userAgent);

        string browser = match.Success
            ? match.Groups[1].Value switch
            {
                "Chrome" or "CriOS" => "Chrome",
                "Edg" or "EdgA" or "EdgiOS" => "Edge",
                "Firefox" or "FxiOS" => "Firefox",
                "OPR" => "Opera",
                _ => "Safari"
            }
            : "Unknown";

        if (match.Success)
            browser += " " + match.Groups[2].Value;

        string operatingSystem = userAgent switch
        {
            _ when Contains(userAgent, "Android") => "Android",
            _ when Contains(userAgent, "CrOS") => "ChromeOS",
            _ when Contains(userAgent, "iPad") || Contains(userAgent, "iPhone") => "iOS",
            _ when Contains(userAgent, "Linux") => "Linux",
            _ when Contains(userAgent, "Mac OS X") || Contains(userAgent, "Macintosh") => "macOS",
            _ when Contains(userAgent, "Windows") => "Windows",
            _ => "Unknown"
        };

        string deviceType = operatingSystem is "Unknown" ? "Unknown" : "Desktop";

        if (Contains(userAgent, "iPad") || (operatingSystem is "Android" && !Contains(userAgent, "Mobile")))
            deviceType = "Tablet";
        else if (Contains(userAgent, "Mobile") || Contains(userAgent, "iPhone"))
            deviceType = "Mobile";

        string? ipAddressValue = null;

        if (!IPAddress.TryParse(remoteIpAddress, out IPAddress? ipAddress))
            return new TrackedDetails(browser, operatingSystem, deviceType, ipAddressValue);

        if (ipAddress.IsIPv4MappedToIPv6)
            ipAddress = ipAddress.MapToIPv4();

        ipAddressValue = ipAddress.ToString();

        return new TrackedDetails(browser, operatingSystem, deviceType, ipAddressValue);
    }

    [GeneratedRegex(
        "(Chrome|CriOS|Edg|EdgA|EdgiOS|Firefox|FxiOS|OPR|Version)/([0-9]{1,3})",
        RegexOptions.RightToLeft,
        100)]
    private static partial Regex BrowserPattern();

    private static bool Contains(string userAgent, string value) =>
        userAgent.Contains(value, StringComparison.OrdinalIgnoreCase);
}
