using System.Diagnostics.Metrics;

namespace AspireBoot.Domain.Meters;

public static class UsersMeter
{
    public static Meter Meter { get; } = new(AppSettings.AppName, AppSettings.AppVersion);

    public static readonly Counter<int> UsersListRequestsCounter =
        Meter.CreateCounter<int>("users_list_requests_counter");
}
