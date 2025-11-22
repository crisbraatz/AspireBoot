using System.Diagnostics.Metrics;

namespace AspireBoot.Domain.Meters;

public static class UsersMeter
{
    public static Meter Meter { get; } = new(AppSettings.AppName, AppSettings.AppVersion);

    public static readonly Counter<int>
        UserListRequestsCounter = Meter.CreateCounter<int>("user_list_requests_counter");
}
