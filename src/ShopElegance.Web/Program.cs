using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Scrutor;
using Serilog;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Core.Validators;
using ShopElegance.Infrastructure;
using ShopElegance.Infrastructure.Caching;
using ShopElegance.Infrastructure.Data;
using ShopElegance.Infrastructure.Data.Seeders;
using ShopElegance.Infrastructure.Email;
using ShopElegance.Infrastructure.Services;
using ShopElegance.Web.Logging;
using ShopElegance.Web.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
    .SetApplicationName("Practora");
    var egyptCulture = new System.Globalization.CultureInfo("en-US");
    egyptCulture.NumberFormat.CurrencySymbol = "EGP ";
    System.Globalization.CultureInfo.DefaultThreadCurrentCulture = egyptCulture;
    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = egyptCulture;

    builder.Host.UseSerilog((ctx, services, cfg) =>
        SerilogConfiguration.Configure(cfg, ctx.Configuration, ctx.HostingEnvironment.EnvironmentName),
        preserveStaticLogger: true);

    builder.Services.AddControllersWithViews(opts =>
        opts.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
    builder.Services.AddRazorPages();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(opts =>
    {
        opts.IdleTimeout = TimeSpan.FromMinutes(30);
        opts.Cookie.HttpOnly = true;
        opts.Cookie.IsEssential = true;
        // 🟢 تعديل 1: السماح بالعمل على HTTP
        opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opts.Cookie.SameSite = SameSiteMode.Lax;
        opts.Cookie.Name = ".Practora.Session";
    });

    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCaching();
    builder.Services.AddResponseCompression(opts => opts.EnableForHttps = true);

    bool useRedis = !string.IsNullOrEmpty(builder.Configuration["Redis:ConnectionString"]);
    if (useRedis)
    {
        builder.Services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = builder.Configuration["Redis:ConnectionString"]);
        builder.Services.AddScoped<ICacheService, DistributedCacheService>();
    }
    else
    {
        builder.Services.AddScoped<ICacheService, MemoryCacheService>();
    }

    var webRootPath = builder.Environment.WebRootPath
                      ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    builder.Services.AddInfrastructure(builder.Configuration, webRootPath);
    builder.Services.Decorate<IProductService, CachedProductService>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.Password.RequiredLength = 8;
        opts.Password.RequireDigit = true;
        opts.Password.RequireUppercase = true;
        opts.Password.RequireLowercase = true;
        opts.Password.RequireNonAlphanumeric = false;
        opts.Lockout.MaxFailedAccessAttempts = 5;
        opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opts.User.RequireUniqueEmail = true;
        opts.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(opts =>
    {
        opts.LoginPath = "/Account/Login";
        opts.LogoutPath = "/Account/Logout";
        opts.AccessDeniedPath = "/Account/AccessDenied";
        opts.ExpireTimeSpan = TimeSpan.FromDays(30);
        opts.SlidingExpiration = true;
        opts.Cookie.HttpOnly = true;
        // 🟢 تعديل 2: السماح بالعمل بدون SSL
        opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opts.Cookie.SameSite = SameSiteMode.Lax;
        opts.Cookie.Name = ".Practora.Auth";
    });

    builder.Services.AddAntiforgery(opts =>
    {
        opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opts.Cookie.SameSite = SameSiteMode.Lax;
        opts.Cookie.HttpOnly = true;
        opts.HeaderName = "X-CSRF-TOKEN";
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 🟢 تعديل 3: نقل Swagger للقمة قبل أي حماية أو Redirect
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Practora API v1");
        c.RoutePrefix = "swagger";
    });

    var supportedCultures = new[] { egyptCulture };
    app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
    {
        DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(egyptCulture),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    });

    app.UseMiddleware<MaintenanceModeMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var maxAge = ctx.File.Name.EndsWith(".css") || ctx.File.Name.EndsWith(".js") ? 604800 : 86400;
            ctx.Context.Response.Headers["Cache-Control"] = $"public,max-age={maxAge}";
        }
    });

    app.UseRouting();
    app.UseResponseCaching();
    app.UseSession();
    app.UseMiddleware<SessionTimeoutMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging(opts =>
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0}ms");

    app.MapHealthChecks("/health");
    app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

    if (app.Environment.EnvironmentName != "Testing")
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while applying database migrations.");
            }
        }
    }

    if (app.Environment.EnvironmentName != "Testing")
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 🟢 فحص جدول الأقسام: لن يتم تشغيل الـ Seeder إلا إذا كانت الداتا بيز فارغة تماماً
            if (!await context.Categories.AnyAsync())
            {
                await DatabaseSeeder.SeedAsync(app.Services);
            }
        }
    }

    Log.Information("Practora started on {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }