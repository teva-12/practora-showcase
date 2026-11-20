using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Tests;

// ──────────────────────────────────────────────
//  In-Memory DB Factory
// ──────────────────────────────────────────────
public static class DbContextFactory
{
    public static ApplicationDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var ctx = new ApplicationDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public static ApplicationDbContext CreateWithSeed(string? dbName = null)
    {
        var ctx = Create(dbName);
        SeedTestData(ctx);
        return ctx;
    }

    private static void SeedTestData(ApplicationDbContext ctx)
    {
        var category = new Category
        {
            Id = 1, Name = "Electronics", Slug = "electronics",
            IsActive = true, SortOrder = 1
        };
        ctx.Categories.Add(category);

        var products = TestDataBuilder.ProductFaker.Generate(10);
        products.ForEach(p => { p.CategoryId = 1; p.Category = category; });
        ctx.Products.AddRange(products);

        ctx.SaveChanges();
    }
}

// ──────────────────────────────────────────────
//  Bogus Fakers — realistic test data
// ──────────────────────────────────────────────
public static class TestDataBuilder
{
    public static Faker<Product> ProductFaker => new Faker<Product>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Slug, (f, p) => p.Name.ToLower().Replace(" ", "-"))
        .RuleFor(p => p.Description, f => f.Lorem.Paragraphs(2))
        .RuleFor(p => p.ShortDescription, f => f.Lorem.Sentence())
        .RuleFor(p => p.Price, f => f.Random.Decimal(9.99m, 999.99m))
        .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 200))
        .RuleFor(p => p.IsActive, f => f.Random.Bool(0.8f))
        .RuleFor(p => p.IsFeatured, f => f.Random.Bool(0.3f))
        .RuleFor(p => p.Type, f => f.PickRandom<ProductType>())
        .RuleFor(p => p.SKU, f => f.Commerce.Ean8())
        .RuleFor(p => p.AverageRating, f => f.Random.Double(1, 5))
        .RuleFor(p => p.ReviewCount, f => f.Random.Int(0, 300))
        .RuleFor(p => p.SalesCount, f => f.Random.Int(0, 1000))
        .RuleFor(p => p.CategoryId, _ => 1)
        .RuleFor(p => p.TrackInventory, _ => true)
        .RuleFor(p => p.CreatedAt, f => f.Date.Past(1));

    public static Faker<Category> CategoryFaker => new Faker<Category>()
        .RuleFor(c => c.Name, f => f.Commerce.Categories(1)[0])
        .RuleFor(c => c.Slug, (f, c) => c.Name.ToLower().Replace(" ", "-"))
        .RuleFor(c => c.Description, f => f.Lorem.Sentence())
        .RuleFor(c => c.IsActive, _ => true)
        .RuleFor(c => c.SortOrder, f => f.Random.Int(1, 10));

    public static Faker<ApplicationUser> UserFaker => new Faker<ApplicationUser>()
        .RuleFor(u => u.FirstName, f => f.Name.FirstName())
        .RuleFor(u => u.LastName, f => f.Name.LastName())
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.UserName, (_, u) => u.Email)
        .RuleFor(u => u.EmailConfirmed, _ => true)
        .RuleFor(u => u.IsActive, _ => true);

    public static Faker<Order> OrderFaker => new Faker<Order>()
        .RuleFor(o => o.OrderNumber, f => $"ORD-{f.Date.Recent().ToString("yyyyMMdd")}-{f.Random.Int(1000, 9999)}")
        .RuleFor(o => o.Status, f => f.PickRandom<OrderStatus>())
        .RuleFor(o => o.PaymentStatus, f => f.PickRandom<PaymentStatus>())
        .RuleFor(o => o.SubTotal, f => f.Random.Decimal(20, 500))
        .RuleFor(o => o.ShippingCost, _ => 9.99m)
        .RuleFor(o => o.TaxAmount, f => f.Random.Decimal(2, 40))
        .RuleFor(o => o.Total, (_, o) => o.SubTotal + o.ShippingCost + o.TaxAmount)
        .RuleFor(o => o.ShippingFirstName, f => f.Name.FirstName())
        .RuleFor(o => o.ShippingLastName, f => f.Name.LastName())
        .RuleFor(o => o.ShippingEmail, f => f.Internet.Email())
        .RuleFor(o => o.ShippingPhone, f => f.Phone.PhoneNumber())
        .RuleFor(o => o.ShippingAddressLine1, f => f.Address.StreetAddress())
        .RuleFor(o => o.ShippingCity, f => f.Address.City())
        .RuleFor(o => o.ShippingState, f => f.Address.State())
        .RuleFor(o => o.ShippingPostalCode, f => f.Address.ZipCode())
        .RuleFor(o => o.ShippingCountry, _ => "United States")
        .RuleFor(o => o.CreatedAt, f => f.Date.Past(1));

    public static Faker<Coupon> CouponFaker => new Faker<Coupon>()
        .RuleFor(c => c.Code, f => f.Random.AlphaNumeric(8).ToUpper())
        .RuleFor(c => c.Type, f => f.PickRandom<CouponType>())
        .RuleFor(c => c.Value, f => f.Random.Decimal(5, 50))
        .RuleFor(c => c.IsActive, _ => true)
        .RuleFor(c => c.UsageCount, _ => 0);
}
