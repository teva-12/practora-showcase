using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Infrastructure.Data;
using ShopElegance.Infrastructure.Repositories;
using ShopElegance.Infrastructure.Services;

namespace ShopElegance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string webRootPath)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                      .EnableRetryOnFailure(maxRetryCount: 3)));

        // Repositories & UoW
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IWhiteCoatVariantService, WhiteCoatVariantService>();
        services.AddScoped<IEmbroideryService, EmbroideryService>();
        services.AddScoped<ILogoService, LogoService>();
        services.AddScoped<IPriceCalculationService, PriceCalculationService>();
        services.AddScoped<ICustomizationPreviewService, CustomizationPreviewService>();
        services.AddScoped<ICustomizationService, CustomizationService>();
        services.AddScoped<IWhiteCoatInventoryService, WhiteCoatInventoryService>();
        services.AddScoped<IShippingCustomizationService, ShippingCustomizationService>();
        // NOTE: IEmailService is intentionally NOT registered here. ShopElegance.Web/Program.cs
        // registers the real ShopElegance.Infrastructure.Email.SmtpEmailService explicitly, after
        // this AddInfrastructure() call runs. LoggingEmailService (this project's Services folder)
        // remains available as an alternative implementation — e.g. for local development without
        // real SMTP credentials — but is not wired up by default to avoid two competing
        // registrations for the same interface.
        services.AddScoped<IShippingCalculationService, EgyptianShippingCalculationService>();

        // File Storage
        services.AddSingleton<IFileStorageService>(_ => new LocalFileStorageService(webRootPath));

        // Caching
        services.AddMemoryCache();
        services.AddResponseCaching();

        return services;
    }
}

// NOTE: Add this extension method call in Program.cs after AddInfrastructure:
// builder.Services.Decorate<IProductService, CachedProductService>();
// This requires the Scrutor NuGet package in the Web project.
