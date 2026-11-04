namespace AspireBoot.Api.Primitives;

public static class ApiConstants
{
    public const string AuthorizationPolicyName = "DefaultAuthorizationPolicy";
    public const string CorsPolicyName = "DefaultCorsPolicy";
    public const string RateLimiterAuthPolicyName = "DefaultRateLimiterAuthPolicy";
    public const string RateLimiterReadsPolicyName = "DefaultRateLimiterReadsPolicy";
    public const string RateLimiterWritesPolicyName = "DefaultRateLimiterWritesPolicy";
    public const string UserIdClaimType = "aspireboot:user-id";
}
