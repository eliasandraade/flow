namespace Flow.API;

public static class RateLimitPolicies
{
    /// <summary>Login, registration and refresh: cheap to abuse, expensive to leave open.</summary>
    public const string Auth = "auth";

    /// <summary>Model-backed endpoints, which cost real money per call.</summary>
    public const string Ai = "ai";
}
