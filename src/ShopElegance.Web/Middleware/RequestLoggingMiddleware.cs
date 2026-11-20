using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;

namespace ShopElegance.Web.Middleware;

/// <summary>
/// Enriches every HTTP request with structured log properties:
/// UserId, RequestId, IP, path, and elapsed ms.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var path = context.Request.Path;

        using (LogContext.PushProperty("RequestId",   requestId))
        using (LogContext.PushProperty("IPAddress",   ip))
        using (LogContext.PushProperty("UserId",      userId))
        using (LogContext.PushProperty("RequestPath", path.Value))
        {
            try
            {
                await _next(context);
                sw.Stop();

                // Log slow requests as warnings
                if (sw.ElapsedMilliseconds > 2000)
                {
                    _logger.LogWarning(
                        "Slow request {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method, path, context.Response.StatusCode, sw.ElapsedMilliseconds);
                }
                else if (context.Response.StatusCode >= 500)
                {
                    _logger.LogError(
                        "Server error {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method, path, context.Response.StatusCode, sw.ElapsedMilliseconds);
                }
                else if (context.Response.StatusCode >= 400)
                {
                    _logger.LogWarning(
                        "Client error {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method, path, context.Response.StatusCode, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Unhandled exception on {Method} {Path} after {ElapsedMs}ms",
                    context.Request.Method, path, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
