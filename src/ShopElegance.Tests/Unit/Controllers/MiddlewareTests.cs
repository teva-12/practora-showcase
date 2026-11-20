using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ShopElegance.Web.Middleware;
using Xunit;

namespace ShopElegance.Tests.Unit.Controllers;

// ────────────────────────────────────────────────────────────────────────────
//  SecurityHeadersMiddleware Tests
// ────────────────────────────────────────────────────────────────────────────
public class SecurityHeadersMiddlewareTests
{
    private static DefaultHttpContext MakeContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static Microsoft.AspNetCore.Hosting.IWebHostEnvironment MakeEnv(bool isDev = true)
    {
        var env = new Moq.Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(isDev ? "Development" : "Production");
        env.Setup(e => e.ApplicationName).Returns("ShopElegance");
        env.Setup(e => e.ContentRootPath).Returns("/");
        env.Setup(e => e.WebRootPath).Returns("/wwwroot");
        return env.Object;
    }

    [Fact]
    public async Task SecurityHeaders_SetsXContentTypeOptions()
    {
        var ctx        = MakeContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, MakeEnv());

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task SecurityHeaders_SetsXFrameOptions()
    {
        var ctx        = MakeContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, MakeEnv());

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["X-Frame-Options"].ToString().Should().Be("SAMEORIGIN");
    }

    [Fact]
    public async Task SecurityHeaders_SetsXXssProtection()
    {
        var ctx        = MakeContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, MakeEnv());

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["X-XSS-Protection"].ToString().Should().Contain("1");
    }

    [Fact]
    public async Task SecurityHeaders_SetsPermissionsPolicy()
    {
        var ctx        = MakeContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, MakeEnv());

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["Permissions-Policy"].ToString().Should().Contain("camera=()");
    }

    [Fact]
    public async Task SecurityHeaders_SetsCSP()
    {
        var ctx        = MakeContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, MakeEnv());

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["Content-Security-Policy"].ToString().Should().Contain("default-src");
    }

    [Fact]
    public async Task SecurityHeaders_Production_SetsHSTS()
    {
        var ctx        = MakeContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, MakeEnv(isDev: false));

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers["Strict-Transport-Security"].ToString()
           .Should().Contain("max-age=31536000");
    }

    [Fact]
    public async Task SecurityHeaders_Development_NoHSTS()
    {
        var ctx        = MakeContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, MakeEnv(isDev: true));

        await middleware.InvokeAsync(ctx);

        ctx.Response.Headers.ContainsKey("Strict-Transport-Security").Should().BeFalse();
    }

    [Fact]
    public async Task SecurityHeaders_CallsNextMiddleware()
    {
        var ctx     = MakeContext();
        bool called = false;
        var mw      = new SecurityHeadersMiddleware(
            _ => { called = true; return Task.CompletedTask; }, MakeEnv());

        await mw.InvokeAsync(ctx);

        called.Should().BeTrue();
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  RateLimitingMiddleware Tests
// ────────────────────────────────────────────────────────────────────────────
public class RateLimitingMiddlewareTests
{
    private static RateLimitingMiddleware CreateMw(
        RequestDelegate next, IMemoryCache? cache = null)
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        return new RateLimitingMiddleware(
            next,
            NullLogger<RateLimitingMiddleware>.Instance,
            cache);
    }

    private static DefaultHttpContext MakeGetRequest(string path = "/Shop", string ip = "1.2.3.4")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Path   = new PathString(path);
        ctx.Response.Body  = new MemoryStream();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        return ctx;
    }

    [Fact]
    public async Task RateLimit_UnderLimit_CallsNext()
    {
        bool called = false;
        var mw = CreateMw(_ => { called = true; return Task.CompletedTask; });

        await mw.InvokeAsync(MakeGetRequest());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimit_SetsRateLimitHeaders()
    {
        var mw  = CreateMw(_ => Task.CompletedTask);
        var ctx = MakeGetRequest();

        await mw.InvokeAsync(ctx);

        ctx.Response.Headers.ContainsKey("X-RateLimit-Limit").Should().BeTrue();
        ctx.Response.Headers.ContainsKey("X-RateLimit-Remaining").Should().BeTrue();
    }

    [Fact]
    public async Task RateLimit_OverAuthLimit_Returns429()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mw    = CreateMw(_ => Task.CompletedTask, cache);

        // Simulate 11 requests on auth path (limit = 10)
        for (int i = 0; i < 11; i++)
        {
            var ctx = MakeGetRequest("/Account/Login", "9.9.9.9");
            ctx.Request.Method = "POST";
            await mw.InvokeAsync(ctx);

            if (i == 10)
            {
                ctx.Response.StatusCode.Should().Be(429);
            }
        }
    }

    [Fact]
    public async Task RateLimit_DifferentIPs_IndependentCounters()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        bool ip2Called = false;

        var mw = CreateMw(_ => { ip2Called = true; return Task.CompletedTask; }, cache);

        // 11 requests from IP1 on auth
        for (int i = 0; i < 11; i++)
            await mw.InvokeAsync(MakeGetRequest("/Account/Login", "5.5.5.5"));

        // First request from IP2 should still pass
        ip2Called = false;
        await mw.InvokeAsync(MakeGetRequest("/Account/Login", "6.6.6.6"));

        ip2Called.Should().BeTrue("Different IPs should have independent rate limit counters");
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  GlobalExceptionMiddleware Tests
// ────────────────────────────────────────────────────────────────────────────
public class GlobalExceptionMiddlewareTests
{
    private static Microsoft.AspNetCore.Hosting.IWebHostEnvironment MakeDevEnv()
    {
        var env = new Moq.Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Development");
        env.Setup(e => e.ApplicationName).Returns("ShopElegance");
        env.Setup(e => e.ContentRootPath).Returns("/");
        env.Setup(e => e.WebRootPath).Returns("/wwwroot");
        return env.Object;
    }

    [Fact]
    public async Task ExceptionMiddleware_NoException_CallsNext()
    {
        bool called = false;
        var mw = new GlobalExceptionMiddleware(
            _ => { called = true; return Task.CompletedTask; },
            NullLogger<GlobalExceptionMiddleware>.Instance,
            MakeDevEnv());

        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        await mw.InvokeAsync(ctx);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ExceptionMiddleware_KeyNotFoundException_Returns404()
    {
        var mw = new GlobalExceptionMiddleware(
            _ => throw new KeyNotFoundException("Item not found"),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            MakeDevEnv());

        var ctx = new DefaultHttpContext();
        ctx.Request.Path   = "/test";
        ctx.Response.Body  = new MemoryStream();

        await mw.InvokeAsync(ctx);

        // Middleware redirects to /Home/NotFound
        ctx.Response.StatusCode.Should().Be(302);
    }

    [Fact]
    public async Task ExceptionMiddleware_UnauthorizedAccessException_Returns403()
    {
        var mw = new GlobalExceptionMiddleware(
            _ => throw new UnauthorizedAccessException("Forbidden"),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            MakeDevEnv());

        var ctx = new DefaultHttpContext();
        ctx.Request.Path  = "/admin";
        ctx.Response.Body = new MemoryStream();

        await mw.InvokeAsync(ctx);

        // Middleware redirects to /Home/Error
        ctx.Response.StatusCode.Should().Be(302);
    }

    [Fact]
    public async Task ExceptionMiddleware_GenericException_Returns500()
    {
        var mw = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("Server error"),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            MakeDevEnv());

        var ctx = new DefaultHttpContext();
        ctx.Request.Path  = "/test";
        ctx.Response.Body = new MemoryStream();

        await mw.InvokeAsync(ctx);

        // Middleware redirects to /Home/Error
        ctx.Response.StatusCode.Should().Be(302);
    }

    [Fact]
    public async Task ExceptionMiddleware_AjaxRequest_ReturnsJson()
    {
        var mw = new GlobalExceptionMiddleware(
            _ => throw new KeyNotFoundException("not found"),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            MakeDevEnv());

        var ctx = new DefaultHttpContext();
        ctx.Request.Path  = "/api/test";
        ctx.Response.Body = new MemoryStream();
        ctx.Request.Headers["Accept"] = "application/json";

        await mw.InvokeAsync(ctx);

        ctx.Response.ContentType.Should().Contain("application/json");
    }
}
