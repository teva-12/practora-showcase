namespace ShopElegance.Web.Middleware;

/// <summary>
/// Adds all OWASP-recommended HTTP security headers on every response.
/// Covers: CSP, HSTS, X-Frame-Options, X-Content-Type-Options,
///         Referrer-Policy, Permissions-Policy, and clears server banner.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env  = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Remove server identification header
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        // X-Content-Type-Options — prevents MIME sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options — clickjacking protection
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

        // X-XSS-Protection — legacy browsers
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions-Policy — deny unnecessary browser APIs
        context.Response.Headers["Permissions-Policy"] =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), " +
            "microphone=(), payment=(), usb=()";

        // HSTS — only in production
        if (!_env.IsDevelopment())
        {
            context.Response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains; preload";
        }

        // Content-Security-Policy
        var csp = BuildCsp(_env.IsDevelopment());
        context.Response.Headers["Content-Security-Policy"] = csp;

        // Cache-Control for HTML pages
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            context.Response.Headers["Pragma"] = "no-cache";
        }

        await _next(context);
    }

    private static string BuildCsp(bool isDevelopment)
    {
        // In development allow 'unsafe-eval' for hot-reload and inline scripts for Razor
        var scriptSrc = isDevelopment
            ? "'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net"
            : "'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net";

        return string.Join("; ", new[]
        {
            "default-src 'self'",
            $"script-src {scriptSrc}",
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com",
            "font-src 'self' https://fonts.gstatic.com",
            "img-src 'self' data: blob: https:",
            "connect-src 'self'",
            "frame-ancestors 'self'",
            "form-action 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "upgrade-insecure-requests"
        });
    }
}
