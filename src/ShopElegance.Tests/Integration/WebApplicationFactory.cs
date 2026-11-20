using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Tests.Integration;

// ─── Custom WebApplicationFactory ────────────────────────────────────────────
public class ShopEleganceWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureServices(services =>
        {
            // Replace real SQL Server with InMemory DB for tests
            var descriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("ApplicationDbContext") == true ||
                d.ImplementationType?.FullName?.Contains("ApplicationDbContext") == true).ToList();

            foreach (var d in descriptors) services.Remove(d);

            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Configure Identity for testing without re-registering already registered schemes
            services.AddIdentityCore<ApplicationUser>(options => { })
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure DB is created and seeded
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        SeedIntegrationData(db);

        return host;
    }

    private static void SeedIntegrationData(ApplicationDbContext db)
    {
        // Always attempt to seed. For InMemory, ensure it is clean.
        if (db.Categories.Any(c => c.Slug == "electronics")) return;

        var cat = new Category { Id = 1, Name = "Electronics", Slug = "electronics", IsActive = true, SortOrder = 1 };
        db.Categories.Add(cat);

        db.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Test Headphones",
                Slug = "test-headphones",
                Description = "Great sound quality with noise cancellation.",
                ShortDescription = "Premium wireless headphones",
                Price = 149.99m,
                StockQuantity = 20,
                IsActive = true,
                IsFeatured = true,
                Type = ProductType.Physical,
                CategoryId = 1,
                SalesCount = 50,
                AverageRating = 4.5,
                ReviewCount = 12
            },
            new Product
            {
                Id = 2,
                Name = "Digital Course",
                Slug = "digital-course",
                Description = "Complete programming course with 40+ hours content.",
                ShortDescription = "Learn to code from scratch",
                Price = 49.99m,
                StockQuantity = 9999,
                IsActive = true,
                Type = ProductType.Digital,
                DownloadUrl = "/downloads/course",
                CategoryId = 1,
                SalesCount = 200
            }
        );

        db.Coupons.Add(new Coupon
        {
            Code = "TEST10",
            Type = CouponType.Percentage,
            Value = 10,
            IsActive = true
        });

        db.SaveChanges();
    }
}