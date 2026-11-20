using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net;

namespace ShopElegance.Web.Middleware;

/// <summary>
/// Sliding-window rate limiter.
/// Blocks IPs that exceed the configured request limit within the time window.
/// Extra-strict on auth endpoints to defend against brute-force attacks.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IMemoryCache _cache;

    // General: 300 req / 60 s per IP
    private const int GeneralLimit  = 300;
    private const int GeneralWindow = 60;

    // Auth endpoints: 10 req / 60 s per IP
    private const int AuthLimit  = 10;
    private const int AuthWindow = 60;

    private static readonly string[] AuthPaths =
        { "/Account/Login", "/Account/Register", "/Account/ForgotPassword" };

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IMemoryCache cache)
    {
        _next   = next;
        _logger = logger;
        _cache  = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip   = GetClientIp(context);
        var path = context.Request.Path.Value ?? "/";
        bool isAuthPath = AuthPaths.Any(a => path.StartsWith(a, StringComparison.OrdinalIgnoreCase));

        int limit  = isAuthPath ? AuthLimit  : GeneralLimit;
        int window = isAuthPath ? AuthWindow : GeneralWindow;

        var key     = $"rl:{ip}:{(isAuthPath ? "auth" : "general")}";
        var counter = _cache.GetOrCreate(key, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(window);
            return new RateCounter();
        })!;

        lock (counter)
        {
            counter.Count++;
        }

        if (counter.Count > limit)
        {
            _logger.LogWarning(
                "Rate limit exceeded for IP {IP} on path {Path}. Count: {Count}/{Limit}",
                ip, path, counter.Count, limit);

            context.Response.StatusCode  = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"]   = window.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                """{"error":"Too many requests. Please try again later."}""");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"]     = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] =
            Math.Max(0, limit - counter.Count).ToString();

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // SECURITY: previously this trusted a client-supplied X-Forwarded-For header directly,
        // which lets anyone bypass IP-based rate limiting simply by sending a different value on
        // every request (the header is attacker-controlled unless a trusted reverse proxy is in
        // front of Kestrel and guaranteed to overwrite it). RemoteIpAddress reflects the actual
        // TCP peer and can't be spoofed by the client.
        //
        // If Practora is deployed behind a reverse proxy / load balancer (IIS, Nginx, Cloudflare,
        // an Azure/AWS load balancer, etc.), add ASP.NET Core's built-in ForwardedHeadersMiddleware
        // in Program.cs — configured with the proxy's real IP/network in KnownProxies/KnownNetworks
        // — before this middleware runs. That validates the header against a trusted source and
        // rewrites HttpContext.Connection.RemoteIpAddress for you, so this method keeps working
        // correctly without needing to parse the header itself.
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class RateCounter { public int Count { get; set; } }
}

/// <summary>
/// Tracks failed login attempts per IP and locks out after threshold.
/// </summary>
public class BruteForceProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BruteForceProtectionMiddleware> _logger;
    private readonly IMemoryCache _cache;

    private const int MaxAttempts   = 5;
    private const int LockoutMinutes = 15;

    public BruteForceProtectionMiddleware(
        RequestDelegate next,
        ILogger<BruteForceProtectionMiddleware> logger,
        IMemoryCache cache)
    {
        _next   = next;
        _logger = logger;
        _cache  = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        bool isLoginPost = path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase)
                        && context.Request.Method == HttpMethods.Post;

        if (!isLoginPost)
        {
            await _next(context);
            return;
        }

        var ip      = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var lockKey = $"brute:lock:{ip}";
        var cntKey  = $"brute:cnt:{ip}";

        if (_cache.TryGetValue(lockKey, out _))
        {
            _logger.LogWarning("Blocked locked-out IP {IP} on login endpoint", ip);
            context.Response.StatusCode  = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(
                "<h2>Too many failed login attempts. Please wait 15 minutes.</h2>");
            return;
        }

        await _next(context);

        // Check if login failed (Identity returns 200 with form errors)
        bool loginFailed = context.Response.StatusCode == 200
                        && context.Items.ContainsKey("LoginFailed");

        if (loginFailed)
        {
            var attempts = _cache.GetOrCreate(cntKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LockoutMinutes);
                return 0;
            });

            attempts++;
            _cache.Set(cntKey, attempts,
                TimeSpan.FromMinutes(LockoutMinutes));

            if (attempts >= MaxAttempts)
            {
                _cache.Set(lockKey, true,
                    TimeSpan.FromMinutes(LockoutMinutes));
                _logger.LogWarning(
                    "IP {IP} locked out after {Max} failed login attempts", ip, MaxAttempts);
            }
        }
    }
}
