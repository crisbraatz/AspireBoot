using AspireBoot.Core.Identity.Sessions;

namespace AspireBoot.Tests.Unit.Core.Identity.Sessions;

public class TrackedDetailsTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.1", "Edge 130", "Windows", "Desktop")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 Version/18.1 Safari/605.1.15", "Safari 18", "macOS", "Desktop")]
    [InlineData("Mozilla/5.0 (Linux; Android 14) Chrome/130.0.0.0 Mobile Safari/537.36", "Chrome 130", "Android", "Mobile")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) FxiOS/132.0 Mobile/15E148 Safari/605.1.15", "Firefox 132", "iOS", "Mobile")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 18_0 like Mac OS X) Version/18.0 Mobile/15E148 Safari/604.1", "Safari 18", "iOS", "Tablet")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) Firefox/132.0", "Firefox 132", "Linux", "Desktop")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) Chrome/130.0 Safari/537.36 OPR/114.0", "Opera 114", "Windows", "Desktop")]
    [InlineData("Mozilla/5.0 (CrOS x86_64) Chrome/130.0", "Chrome 130", "ChromeOS", "Desktop")]
    [InlineData("Mozilla/5.0 (Linux; Android 14) Chrome/130.0", "Chrome 130", "Android", "Tablet")]
    [InlineData("Mozilla/5.0 (iPhone) CriOS/130.0 Mobile", "Chrome 130", "iOS", "Mobile")]
    [InlineData("Mozilla/5.0 (Android) EdgA/130.0 Mobile", "Edge 130", "Android", "Mobile")]
    [InlineData("Mozilla/5.0 (iPhone) EdgiOS/130.0", "Edge 130", "iOS", "Mobile")]
    [InlineData("Mozilla/5.0 (Macintosh) Version/18.1", "Safari 18", "macOS", "Desktop")]
    public void ShouldReturnTrackedDetailsAndRecognizeCommonDevicesWithoutExternalRequests(string userAgent, string browser, string operatingSystem, string deviceType)
    {
        TrackedDetails trackedDetails = TrackedDetails.Create(userAgent, "::ffff:203.0.113.7");

        Assert.Equal(new TrackedDetails(browser, operatingSystem, deviceType, "203.0.113.7"), trackedDetails);
    }

    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8::1", "2001:db8::1")]
    [InlineData(null, null)]
    public void ShouldReturnTrackedDetailsWhenNormalizingNativeOrMissingIpAddresses(string? ipAddress = null, string? expected = null)
    {
        TrackedDetails trackedDetails = TrackedDetails.Create("Chrome/130.0", ipAddress);

        Assert.Equal(expected, trackedDetails.RemoteIpAddress);
        Assert.Equal("Chrome 130", trackedDetails.Browser);
    }

    [Theory]
    [InlineData(0, "not-an-ip", null)]
    [InlineData(1025, "::1", "::1")]
    public void ShouldReturnTrackedDetailsWhenHandleMissingInvalidAndOversizedClientInformation(int length, string ip, string? expectedIp = null)
    {
        TrackedDetails trackedDetails = TrackedDetails.Create(new string('x', length), ip);

        Assert.Equal(new TrackedDetails("Unknown", "Unknown", "Unknown", expectedIp), trackedDetails);
    }

    [Fact]
    public void ShouldReturnTrackedDetailsWhenTreatAnAbsentUserAgentAsUnknown()
    {
        TrackedDetails trackedDetails = TrackedDetails.Create(null!);

        Assert.Equal(new TrackedDetails("Unknown", "Unknown", "Unknown"), trackedDetails);
    }
}
