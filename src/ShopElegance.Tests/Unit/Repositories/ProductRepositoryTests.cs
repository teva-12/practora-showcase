using FluentAssertions;
using ShopElegance.Core.Entities;
using ShopElegance.Infrastructure.Repositories;
using Xunit;

namespace ShopElegance.Tests.Unit.Repositories;

public class ProductRepositoryTests
{
    // ─── GetBySlugAsync ──────────────────────────────────────────────────
    [Fact]
    public async Task GetBySlugAsync_ExistingSlug_ReturnsProduct()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);
        var product = ctx.Products.First();

        var result = await repo.GetBySlugAsync(product.Slug);

        result.Should().NotBeNull();
        result!.Slug.Should().Be(product.Slug);
        result.Category.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_NonExistingSlug_ReturnsNull()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);

        var result = await repo.GetBySlugAsync("does-not-exist");

        result.Should().BeNull();
    }

    // ─── SlugExistsAsync ─────────────────────────────────────────────────
    [Fact]
    public async Task SlugExistsAsync_ExistingSlug_ReturnsTrue()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);
        var slug = ctx.Products.First().Slug;

        var result = await repo.SlugExistsAsync(slug);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SlugExistsAsync_ExcludingSelf_ReturnsFalse()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);
        var product = ctx.Products.First();

        var result = await repo.SlugExistsAsync(product.Slug, product.Id);

        result.Should().BeFalse();
    }

    // ─── GetPagedAsync ───────────────────────────────────────────────────
    [Fact]
    public async Task GetPagedAsync_FirstPage_ReturnsCorrectCount()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);

        var (products, total) = await repo.GetPagedAsync(1, 5);

        products.Should().HaveCount(5);
        total.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsRemainder()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);

        var (products, total) = await repo.GetPagedAsync(2, 5);

        products.Should().HaveCount(5);
        total.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByCategoryId_ReturnsOnlyThatCategory()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);

        var (products, total) = await repo.GetPagedAsync(1, 20, categoryId: 1);

        products.Should().AllSatisfy(p => p.CategoryId.Should().Be(1));
        total.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_SearchByName_FiltersCorrectly()
    {
        using var ctx = DbContextFactory.Create();
        var cat = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, SortOrder = 1 };
        ctx.Categories.Add(cat);
        var product = new Product
        {
            Name = "Unique Banana Widget", Slug = "unique-banana-widget",
            Description = "desc", ShortDescription = "short",
            Price = 10m, StockQuantity = 5, CategoryId = 1, IsActive = true
        };
        ctx.Products.Add(product);
        ctx.Products.AddRange(TestDataBuilder.ProductFaker.Generate(5).Select(p => { p.CategoryId = 1; return p; }));
        await ctx.SaveChangesAsync();

        var repo = new ProductRepository(ctx);
        var (results, total) = await repo.GetPagedAsync(1, 20, search: "Banana");

        results.Should().HaveCount(1);
        results.First().Name.Should().Contain("Banana");
    }

    [Fact]
    public async Task GetPagedAsync_PriceRange_FiltersCorrectly()
    {
        using var ctx = DbContextFactory.Create();
        var cat = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, SortOrder = 1 };
        ctx.Categories.Add(cat);
        ctx.Products.AddRange(new[]
        {
            new Product { Name = "Cheap", Slug = "cheap", Description="d", ShortDescription="s", Price=10m, StockQuantity=5, CategoryId=1, IsActive=true },
            new Product { Name = "Mid",   Slug = "mid",   Description="d", ShortDescription="s", Price=50m, StockQuantity=5, CategoryId=1, IsActive=true },
            new Product { Name = "Pricy", Slug = "pricy", Description="d", ShortDescription="s", Price=200m, StockQuantity=5, CategoryId=1, IsActive=true }
        });
        await ctx.SaveChangesAsync();

        var repo = new ProductRepository(ctx);
        var (results, _) = await repo.GetPagedAsync(1, 20, minPrice: 20m, maxPrice: 100m);

        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Mid");
    }

    // ─── GetFeaturedAsync ─────────────────────────────────────────────────
    [Fact]
    public async Task GetFeaturedAsync_ReturnsOnlyFeaturedActive()
    {
        using var ctx = DbContextFactory.Create();
        var cat = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, SortOrder = 1 };
        ctx.Categories.Add(cat);
        ctx.Products.AddRange(new[]
        {
            new Product { Name="Featured Active", Slug="fa", Description="d", ShortDescription="s", Price=10m, StockQuantity=5, CategoryId=1, IsActive=true,  IsFeatured=true },
            new Product { Name="Featured Inactive",Slug="fi",Description="d", ShortDescription="s", Price=10m, StockQuantity=5, CategoryId=1, IsActive=false, IsFeatured=true },
            new Product { Name="Normal",           Slug="n", Description="d", ShortDescription="s", Price=10m, StockQuantity=5, CategoryId=1, IsActive=true,  IsFeatured=false }
        });
        await ctx.SaveChangesAsync();

        var repo = new ProductRepository(ctx);
        var result = await repo.GetFeaturedAsync(10);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Featured Active");
    }

    // ─── GetLowStockAsync ────────────────────────────────────────────────
    [Fact]
    public async Task GetLowStockAsync_ReturnsProductsBelowThreshold()
    {
        using var ctx = DbContextFactory.Create();
        var cat = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, SortOrder = 1 };
        ctx.Categories.Add(cat);
        ctx.Products.AddRange(new[]
        {
            new Product { Name="Low",    Slug="low",  Description="d", ShortDescription="s", Price=10m, StockQuantity=2,  CategoryId=1, IsActive=true, TrackInventory=true },
            new Product { Name="Normal", Slug="norm", Description="d", ShortDescription="s", Price=10m, StockQuantity=50, CategoryId=1, IsActive=true, TrackInventory=true }
        });
        await ctx.SaveChangesAsync();

        var repo = new ProductRepository(ctx);
        var result = await repo.GetLowStockAsync(5);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Low");
    }

    // ─── Generic AddAsync / Update / Remove ──────────────────────────────
    [Fact]
    public async Task AddAsync_NewProduct_PersistsToDatabase()
    {
        using var ctx = DbContextFactory.Create();
        var cat = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, SortOrder = 1 };
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();

        var repo = new ProductRepository(ctx);
        var product = new Product
        {
            Name = "New", Slug = "new", Description = "d", ShortDescription = "s",
            Price = 50m, StockQuantity = 10, CategoryId = 1, IsActive = true
        };

        await repo.AddAsync(product);
        await ctx.SaveChangesAsync();

        ctx.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task CountAsync_NoPredicate_ReturnsTotal()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);

        var count = await repo.CountAsync();

        count.Should().Be(10);
    }

    [Fact]
    public async Task AnyAsync_MatchingPredicate_ReturnsTrue()
    {
        using var ctx = DbContextFactory.CreateWithSeed();
        var repo = new ProductRepository(ctx);
        var slug = ctx.Products.First().Slug;

        var exists = await repo.AnyAsync(p => p.Slug == slug);

        exists.Should().BeTrue();
    }
}
