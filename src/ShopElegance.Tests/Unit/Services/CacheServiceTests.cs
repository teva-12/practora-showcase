using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ShopElegance.Infrastructure.Caching;
using ShopElegance.Infrastructure.Services;
using Xunit;
using Moq;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Tests.Unit.Services;

// ────────────────────────────────────────────────────────────────────────────
//  MemoryCacheService Tests
// ────────────────────────────────────────────────────────────────────────────
public class MemoryCacheServiceTests
{
    private static MemoryCacheService CreateSut()
    {
        var cache  = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<MemoryCacheService>.Instance;
        return new MemoryCacheService(cache, logger);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        var sut = CreateSut();
        var dto = new ProductListDto { Id = 1, Name = "Widget", Slug = "widget", Price = 9.99m };

        await sut.SetAsync("test:1", dto, TimeSpan.FromMinutes(5));
        var result = await sut.GetAsync<ProductListDto>("test:1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Widget");
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsNull()
    {
        var sut    = CreateSut();
        var result = await sut.GetAsync<ProductListDto>("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_DeletesEntry()
    {
        var sut = CreateSut();
        var dto = new ProductListDto { Id = 2, Name = "Thing" };

        await sut.SetAsync("to-remove", dto);
        await sut.RemoveAsync("to-remove");

        var result = await sut.GetAsync<ProductListDto>("to-remove");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_MissingKey_CallsFactory()
    {
        var sut          = CreateSut();
        bool factoryCalled = false;

        var result = await sut.GetOrSetAsync("new-key", async () =>
        {
            factoryCalled = true;
            await Task.Delay(1);
            return new ProductListDto { Id = 99, Name = "FromFactory" };
        });

        factoryCalled.Should().BeTrue();
        result.Id.Should().Be(99);
    }

    [Fact]
    public async Task GetOrSetAsync_ExistingKey_DoesNotCallFactory()
    {
        var sut = CreateSut();
        await sut.SetAsync("cached-key", new ProductListDto { Id = 1, Name = "Cached" });

        bool factoryCalled = false;
        var result = await sut.GetOrSetAsync("cached-key", () =>
        {
            factoryCalled = true;
            return Task.FromResult(new ProductListDto { Id = 2, Name = "FromFactory" });
        });

        factoryCalled.Should().BeFalse();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Cached");
    }

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesAllMatchingKeys()
    {
        var sut = CreateSut();
        await sut.SetAsync("products:1", new ProductListDto { Id = 1, Name = "A" });
        await sut.SetAsync("products:2", new ProductListDto { Id = 2, Name = "B" });
        await sut.SetAsync("categories:1", new CategoryDto { Id = 1, Name = "C" });

        await sut.RemoveByPrefixAsync("products:");

        (await sut.GetAsync<ProductListDto>("products:1")).Should().BeNull();
        (await sut.GetAsync<ProductListDto>("products:2")).Should().BeNull();
        (await sut.GetAsync<CategoryDto>("categories:1")).Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_ExpiredEntry_ReturnsNull()
    {
        var sut = CreateSut();
        await sut.SetAsync("expiring", new ProductListDto { Id = 1 }, TimeSpan.FromMilliseconds(50));
        await Task.Delay(100);
        var result = await sut.GetAsync<ProductListDto>("expiring");
        result.Should().BeNull();
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  CachedProductService Tests
// ────────────────────────────────────────────────────────────────────────────
public class CachedProductServiceTests
{
    private readonly Mock<IProductService> _innerMock;
    private readonly MemoryCacheService _cacheService;
    private readonly CachedProductService _sut;

    public CachedProductServiceTests()
    {
        _innerMock    = new Mock<IProductService>();
        var cache     = new MemoryCache(new MemoryCacheOptions());
        _cacheService = new MemoryCacheService(cache, NullLogger<MemoryCacheService>.Instance);
        _sut          = new CachedProductService(
            _innerMock.Object, _cacheService,
            NullLogger<CachedProductService>.Instance);
    }

    [Fact]
    public async Task GetFeaturedProductsAsync_SecondCall_DoesNotHitInner()
    {
        var products = TestDataBuilder.ProductFaker.Generate(4)
            .Select(p => new ProductListDto { Id = p.Id, Name = p.Name, Slug = p.Slug })
            .ToList();

        _innerMock.Setup(s => s.GetFeaturedProductsAsync(4))
                  .ReturnsAsync(products);

        // First call — hits inner
        var r1 = await _sut.GetFeaturedProductsAsync(4);
        // Second call — from cache
        var r2 = await _sut.GetFeaturedProductsAsync(4);

        _innerMock.Verify(s => s.GetFeaturedProductsAsync(4), Times.Once,
            "Inner service should only be called once; second call served from cache");
        r1.Count().Should().Be(r2.Count());
    }

    [Fact]
    public async Task GetProductBySlugAsync_SecondCall_ServedFromCache()
    {
        var dto = new ProductDetailDto { Id = 1, Name = "Headphones", Slug = "headphones", Price = 299m };
        _innerMock.Setup(s => s.GetProductBySlugAsync("headphones")).ReturnsAsync(dto);

        await _sut.GetProductBySlugAsync("headphones");
        await _sut.GetProductBySlugAsync("headphones");

        _innerMock.Verify(s => s.GetProductBySlugAsync("headphones"), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_InvalidatesCache()
    {
        var dto = new ProductDetailDto { Id = 1, Name = "Updated", Slug = "test", Price = 50m };
        _innerMock.Setup(s => s.GetProductBySlugAsync("test")).ReturnsAsync(dto);
        _innerMock.Setup(s => s.UpdateProductAsync(1, It.IsAny<UpdateProductDto>())).ReturnsAsync(dto);
        _innerMock.Setup(s => s.GetProductByIdAsync(1)).ReturnsAsync(dto);

        // Populate cache
        await _sut.GetProductBySlugAsync("test");

        // Update should invalidate
        await _sut.UpdateProductAsync(1, new UpdateProductDto { Id = 1, Name = "Updated", ProductType = "Physical" });

        // Next call should hit inner again
        await _sut.GetProductBySlugAsync("test");

        _innerMock.Verify(s => s.GetProductBySlugAsync("test"), Times.Exactly(2),
            "Cache should be invalidated after update");
    }

    [Fact]
    public async Task DeleteProductAsync_InvalidatesCache()
    {
        var dto = new ProductDetailDto { Id = 1, Name = "To Delete", Slug = "to-delete", Price = 10m };
        _innerMock.Setup(s => s.GetProductByIdAsync(1)).ReturnsAsync(dto);
        _innerMock.Setup(s => s.GetProductBySlugAsync("to-delete")).ReturnsAsync(dto);
        _innerMock.Setup(s => s.DeleteProductAsync(1)).Returns(Task.CompletedTask);

        await _sut.GetProductBySlugAsync("to-delete");
        await _sut.DeleteProductAsync(1);
        await _sut.GetProductBySlugAsync("to-delete");

        _innerMock.Verify(s => s.GetProductBySlugAsync("to-delete"), Times.Exactly(2));
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  CacheKeys Tests
// ────────────────────────────────────────────────────────────────────────────
public class CacheKeysTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    public void FeaturedProducts_KeyDiffersByCount(int count)
    {
        var key = CacheKeys.FeaturedProducts(count);
        key.Should().Contain(count.ToString());
        key.Should().StartWith("products:");
    }

    [Fact]
    public void ProductBySlug_ContainsSlug()
    {
        var key = CacheKeys.ProductBySlug("my-product");
        key.Should().Contain("my-product");
    }

    [Fact]
    public void ProductById_ContainsId()
    {
        var key = CacheKeys.ProductById(42);
        key.Should().Contain("42");
    }

    [Fact]
    public void CategoryList_IsConstant()
    {
        CacheKeys.CategoryList().Should().Be("categories:all");
    }

    [Fact]
    public void DashboardStats_IsConstant()
    {
        CacheKeys.DashboardStats().Should().Be("dashboard:stats");
    }
}
