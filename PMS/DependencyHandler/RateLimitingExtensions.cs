using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PMS.DependencyHandler;

/// <summary>
/// Constant names for rate limiting policies so controllers can reference them
/// without magic strings.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Applied to all authentication endpoints (login, signup, password reset, etc.).</summary>
    public const string Auth = "AuthPolicy";

    /// <summary>Applied to OTP / 2FA verification endpoints — very strict to prevent brute-force.</summary>
    public const string OtpVerification = "OtpVerificationPolicy";

    /// <summary>Applied globally to all other API endpoints.</summary>
    public const string General = "GeneralPolicy";
}

/// <summary>
/// Helpers for registering and applying IP-based fixed-window rate limiting.
/// Configuration is read from the <c>RateLimiting</c> section in appsettings.json.
///
/// Three named policies are provided:
/// <list type="bullet">
///   <item><description><see cref="RateLimitPolicies.Auth"/> — strict limit for auth endpoints (login, signup, password reset).</description></item>
///   <item><description><see cref="RateLimitPolicies.OtpVerification"/> — ultra-strict limit for OTP / 2FA verification endpoints.</description></item>
///   <item><description><see cref="RateLimitPolicies.General"/> — relaxed limit for all other endpoints.</description></item>
/// </list>
///
/// The partition key is the client IP address (falls back to "unknown" if undetermined).
/// When the limit is exceeded the middleware returns <b>429 Too Many Requests</b> with a
/// <c>Retry-After</c> header and a JSON body.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Registers fixed-window rate limiting services using values from <c>appsettings.json</c>.
    /// </summary>
    public static IServiceCollection AddConfiguredRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authCfg    = configuration.GetSection("RateLimiting:Auth");
        var otpCfg     = configuration.GetSection("RateLimiting:OtpVerification");
        var generalCfg = configuration.GetSection("RateLimiting:General");

        int authPermitLimit    = authCfg.GetValue("PermitLimit",    10);
        int authWindowSeconds  = authCfg.GetValue("WindowSeconds",  60);
        int authQueueLimit     = authCfg.GetValue("QueueLimit",      0);

        int otpPermitLimit    = otpCfg.GetValue("PermitLimit",    5);
        int otpWindowSeconds  = otpCfg.GetValue("WindowSeconds", 300);
        int otpQueueLimit     = otpCfg.GetValue("QueueLimit",      0);

        int generalPermitLimit   = generalCfg.GetValue("PermitLimit",   100);
        int generalWindowSeconds = generalCfg.GetValue("WindowSeconds",  60);
        int generalQueueLimit    = generalCfg.GetValue("QueueLimit",     10);

        services.AddRateLimiter(options =>
        {
            // ── Reject handler — returns 429 with a JSON body ─────────────────
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                // Include Retry-After header if the limiter supplies metadata
                var retryAfterSeconds = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfter)
                    ? (int)retryAfter.TotalSeconds
                    : authWindowSeconds;

                context.HttpContext.Response.Headers["Retry-After"] =
                    retryAfterSeconds.ToString();

                await context.HttpContext.Response.WriteAsync(
                    $"{{\"message\":\"Too many requests. Please wait before retrying.\","
                    + $"\"retryAfterSeconds\":{retryAfterSeconds}}}",
                    cancellationToken);
            };

            // ── Global default — applied to every endpoint without its own policy ──
            // Protects subscription, pharmacy profile, and any new routes automatically.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = generalPermitLimit,
                        Window               = TimeSpan.FromSeconds(generalWindowSeconds),
                        QueueLimit           = generalQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment    = true
                    }));

            // ── Auth policy — strict, no queue ────────────────────────────────
            // Covers: login, signup, create-user, assign-role, change-password,
            //         forgot-password, reset-password
            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = authPermitLimit,
                        Window               = TimeSpan.FromSeconds(authWindowSeconds),
                        QueueLimit           = authQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment    = true
                    }));

            // ── OTP Verification policy — ultra-strict, no queue ──────────────
            // Prevents brute-force of time-based OTP / 2FA codes.
            // Default: 5 attempts per 5-minute window.
            options.AddPolicy(RateLimitPolicies.OtpVerification, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = otpPermitLimit,
                        Window               = TimeSpan.FromSeconds(otpWindowSeconds),
                        QueueLimit           = otpQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment    = true
                    }));

            // ── General policy — relaxed, with a small queue ──────────────────
            options.AddPolicy(RateLimitPolicies.General, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = generalPermitLimit,
                        Window               = TimeSpan.FromSeconds(generalWindowSeconds),
                        QueueLimit           = generalQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment    = true
                    }));
        });

        return services;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the real client IP, honouring X-Forwarded-For when behind a reverse proxy.
    /// Falls back to the direct connection remote IP, then to "unknown".
    /// </summary>
    private static string GetClientIp(HttpContext ctx)
    {
        // X-Forwarded-For contains a comma-separated list; the first entry is the originating client.
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var ip = forwarded.Split(',', StringSplitOptions.TrimEntries)[0];
            if (!string.IsNullOrEmpty(ip)) return ip;
        }

        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
