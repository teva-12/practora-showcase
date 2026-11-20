using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using Xunit;

namespace ShopElegance.Tests.Integration.Database;

public class DatabaseIntegrationTests : IClassFixture<ShopEleganceWebFactory>
{
    private readonly ShopEleganceWebFactory _factory;

    public DatabaseIntegrationTests(ShopEleganceWebFactory factory)
    {
        _factory = factory;
    }

    private IServiceScope CreateScope() => _factory.Services.CreateScope();

    // ─── UnitOfWork / Repository integration ────────────────────────────
    [Fact]
    public async Task UnitOfWork_Products_CanQuerySeededData()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var (products, total) = await uow.Products.GetPagedAsync(1, 10);

        total.Should().BeGreaterThan(0);
        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UnitOfWork_Products_GetBySlug_Works()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var product = await uow.Products.GetBySlugAsync("test-headphones");

        product.Should().NotBeNull();
        product!.Name.Should().BeOneOf("Test Headphones", "Test Headphones Updated");
        product.Price.Should().Be(149.99m);
    }

    [Fact]
    public async Task UnitOfWork_Categories_GetAll_Works()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var categories = await uow.Categories.GetAllAsync();

        categories.Should().NotBeEmpty();
        categories.Any(c => c.Name == "Electronics").Should().BeTrue();
    }

    [Fact]
    public async Task UnitOfWork_Coupons_GetByCode_Works()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var coupon = await uow.Coupons.GetByCodeAsync("TEST10");

        coupon.Should().NotBeNull();
        coupon!.Value.Should().Be(10);
        coupon.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UnitOfWork_Products_Add_Persists()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var newProduct = new Product
        {
            Name = "Integration Test Product",
            Slug = "integration-test-" + Guid.NewGuid().ToString("N")[..8],
            Description = "Created in integration test",
            ShortDescription = "Test product",
            Price = 39.99m,
            StockQuantity = 5,
            CategoryId = 1,
            IsActive = true
        };

        await uow.Products.AddAsync(newProduct);
        await uow.SaveChangesAsync();

        var retrieved = await uow.Products.GetByIdAsync(newProduct.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Integration Test Product");
    }

    [Fact]
    public async Task UnitOfWork_Cart_CreateAndAddItem()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var sessionId = "test-session-" + Guid.NewGuid();
        var cart = new Cart { SessionId = sessionId };
        await uow.Carts.AddAsync(cart);
        await uow.SaveChangesAsync();

        cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = 1, Quantity = 2 });
        await uow.SaveChangesAsync();

        var loaded = await uow.Carts.GetWithItemsAsync(cart.Id);
        loaded.Should().NotBeNull();
        loaded!.Items.Should().HaveCount(1);
        loaded.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task UnitOfWork_Products_FeaturedQuery_ReturnsOnlyFeatured()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var featured = await uow.Products.GetFeaturedAsync(10);

        featured.Should().AllSatisfy(p =>
        {
            p.IsFeatured.Should().BeTrue();
            p.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task UnitOfWork_SaveChanges_UpdatesTimestamp()
    {
        using var scope = CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var product = await uow.Products.GetBySlugAsync("test-headphones");
        product.Should().NotBeNull();

        product!.Name = "Test Headphones Updated";
        uow.Products.Update(product);
        await uow.SaveChangesAsync();

        var updated = await uow.Products.GetBySlugAsync("test-headphones");
        updated!.UpdatedAt.Should().NotBeNull();
        updated.Name.Should().Be("Test Headphones Updated");
    }
}
