namespace AspireBoot.Core;

public static class CoreConstants
{
    public static bool IsDevelopment { get; } =
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?.Equals("Development", StringComparison.OrdinalIgnoreCase) ??
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?.Equals("Development", StringComparison.OrdinalIgnoreCase) ??
        false;
}
