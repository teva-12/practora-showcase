using System.Net;
using System.Text.Json;

namespace ShopElegance.Web.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Forbidden);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;

        if (context.Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            context.Response.ContentType = "application/json";
            var errorResponse = new
            {
                StatusCode = (int)statusCode,
                Message = _env.IsDevelopment() ? ex.Message : "An error occurred.",
                Detail = _env.IsDevelopment() ? ex.StackTrace : null
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
        else
        {
            context.Response.Redirect(statusCode == HttpStatusCode.NotFound ? "/Home/NotFound" : "/Home/Error");
        }
    }
}
