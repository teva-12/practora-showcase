namespace ShopElegance.Web.Middleware;

/// <summary>
/// Sliding session timeout: after 30 minutes of inactivity the session is cleared
/// and the user is redirected to login.
/// </summary>
public class SessionTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionTimeoutMiddleware> _logger;
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(30);
    private const string LastActivityKey = "LastActivity";

    public SessionTimeoutMiddleware(RequestDelegate next, ILogger<SessionTimeoutMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check for authenticated users with an active session
        if (context.User.Identity?.IsAuthenticated == true
            && !IsExemptPath(context.Request.Path))
        {
            var lastActivityStr = context.Session.GetString(LastActivityKey);

            if (lastActivityStr != null
                && DateTime.TryParse(lastActivityStr, out var lastActivity)
                && DateTime.UtcNow - lastActivity > Timeout)
            {
                _logger.LogInformation(
                    "Session timed out for user {User} after {Minutes} minutes of inactivity",
                    context.User.Identity.Name, Timeout.TotalMinutes);

                context.Session.Clear();

                // Don't redirect AJAX/API requests
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        """{"error":"Session expired. Please log in again."}""");
                    return;
                }

                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}&reason=timeout");
                return;
            }

            // Refresh the last activity timestamp
            context.Session.SetString(LastActivityKey, DateTime.UtcNow.ToString("O"));
        }

        await _next(context);
    }

    private static bool IsExemptPath(PathString path) =>
        path.StartsWithSegments("/Account/Login")  ||
        path.StartsWithSegments("/Account/Logout") ||
        path.StartsWithSegments("/css")            ||
        path.StartsWithSegments("/js")             ||
        path.StartsWithSegments("/images")         ||
        path.StartsWithSegments("/lib")            ||
        path.StartsWithSegments("/favicon");
}

/// <summary>
/// Maintenance mode: returns a 503 page when maintenance mode is toggled in config.
/// Whitelisted IPs bypass the maintenance screen.
/// </summary>
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    public MaintenanceModeMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<MaintenanceModeMiddleware> logger)
    {
        _next          = next;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        bool maintenanceEnabled = _configuration.GetValue<bool>("AppSettings:MaintenanceMode");

        if (maintenanceEnabled)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "";
            var whitelist = _configuration.GetSection("AppSettings:MaintenanceWhitelist")
                                          .Get<string[]>() ?? Array.Empty<string>();

            if (!whitelist.Contains(clientIp))
            {
                _logger.LogInformation("Maintenance mode active — blocked request from {IP}", clientIp);
                context.Response.StatusCode  = 503;
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers["Retry-After"] = "3600";

                await context.Response.WriteAsync(MaintenanceHtml());
                return;
            }
        }

        await _next(context);
    }

    private static string MaintenanceHtml() => """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>ShopElegance — Maintenance</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{font-family:'Segoe UI',sans-serif;background:#1a1728;color:#fff;
                 display:flex;align-items:center;justify-content:center;
                 min-height:100vh;text-align:center;padding:24px}
            .card{max-width:500px}
            .icon{font-size:5rem;margin-bottom:24px}
            h1{font-size:2.2rem;margin-bottom:12px;color:#d4a853}
            p{color:rgba(255,255,255,0.7);line-height:1.7;font-size:1.05rem}
          </style>
        </head>
        <body>
          <div class="card">
            <div class="icon">🔧</div>
            <h1>We'll Be Back Soon</h1>
            <p>ShopElegance is undergoing scheduled maintenance.
               We're working hard to improve your experience.<br/><br/>
               Expected downtime: <strong>1 hour</strong>.<br/>
               Thank you for your patience.</p>
          </div>
        </body>
        </html>
        """;
}
