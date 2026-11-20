using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _productRepoMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();

        _uowMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        var tagRepoMock = new Mock<ITagRepository>();
        tagRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>()))
                   .ReturnsAsync((Tag?)null);
        tagRepoMock.Setup(r => r.AddAsync(It.IsAny<Tag>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.Tags).Returns(tagRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _sut = new ProductService(_uowMock.Object, _loggerMock.Object);
    }

    // ─── GetProductBySlugAsync ───────────────────────────────────────────
    [Fact]
    public async Task GetProductBySlugAsync_ExistingSlug_ReturnsProductDto()
    {
        // Arrange
        var product = TestDataBuilder.ProductFaker.Generate();
        product.Category = TestDataBuilder.CategoryFaker.Generate();
        _productRepoMock.Setup(r => r.GetBySlugAsync(product.Slug)).ReturnsAsync(product);

        // Act
        var result = await _sut.GetProductBySlugAsync(product.Slug);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(product.Name);
        result.Slug.Should().Be(product.Slug);
        result.Price.Should().Be(product.Price);
    }

    [Fact]
    public async Task GetProductBySlugAsync_NonExistingSlug_ReturnsNull()
    {
        // Arrange
        _productRepoMock.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.GetProductBySlugAsync("non-existing-slug");

        // Assert
        result.Should().BeNull();
    }

    // ─── GetProductsAsync ────────────────────────────────────────────────
    [Fact]
    public async Task GetProductsAsync_ValidFilter_ReturnsPagedResult()
    {
        // Arrange
        var products = TestDataBuilder.ProductFaker.Generate(5);
        products.ForEach(p => p.Category = TestDataBuilder.CategoryFaker.Generate());

        _productRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>()))
            .ReturnsAsync((products, 5));

        var filter = new ProductFilterDto { Page = 1, PageSize = 12 };

        // Act
        var result = await _sut.GetProductsAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetProductsAsync_EmptyResult_ReturnsEmptyPagedResult()
    {
        // Arrange
        _productRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>()))
            .ReturnsAsync((Enumerable.Empty<Product>(), 0));

        // Act
        var result = await _sut.GetProductsAsync(new ProductFilterDto());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    // ─── CreateProductAsync ──────────────────────────────────────────────
    [Fact]
    public async Task CreateProductAsync_ValidDto_CreatesAndReturnsProduct()
    {
        // Arrange
        _productRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var dto = new CreateProductDto
        {
            Name = "Test Product",
            Description = "A great test product description",
            ShortDescription = "Short desc",
            Price = 99.99m,
            StockQuantity = 50,
            CategoryId = 1,
            ProductType = "Physical",
            IsActive = true
        };

        // Act
        var result = await _sut.CreateProductAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        _productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateProductAsync_WithCompareAtPrice_CalculatesDiscount()
    {
        // Arrange
        _productRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var dto = new CreateProductDto
        {
            Name = "Discounted Product",
            Description = "Has a sale price",
            ShortDescription = "Sale!",
            Price = 75.00m,
            CompareAtPrice = 100.00m,
            StockQuantity = 10,
            CategoryId = 1,
            ProductType = "Physical",
            IsActive = true
        };

        // Act
        var result = await _sut.CreateProductAsync(dto);

        // Assert
        result.DiscountPercentage.Should().Be(25);
    }

    // ─── DeleteProductAsync ──────────────────────────────────────────────
    [Fact]
    public async Task DeleteProductAsync_ExistingProduct_SoftDeletes()
    {
        // Arrange
        var product = TestDataBuilder.ProductFaker.Generate();
        _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        // Act
        await _sut.DeleteProductAsync(product.Id);

        // Assert
        product.IsDeleted.Should().BeTrue();
        product.DeletedAt.Should().NotBeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_NonExistingProduct_ThrowsKeyNotFoundException()
    {
        // Arrange
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = () => _sut.DeleteProductAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── GenerateSlugAsync ───────────────────────────────────────────────
    [Fact]
    public async Task GenerateSlugAsync_UniqueSlug_ReturnsBaseSlug()
    {
        // Arrange
        _productRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);

        // Act
        var slug = await _sut.GenerateSlugAsync("My Awesome Product");

        // Assert
        slug.Should().Be("my-awesome-product");
    }

    [Fact]
    public async Task GenerateSlugAsync_DuplicateSlug_AppendsCounter()
    {
        // Arrange
        _productRepoMock.SetupSequence(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(true)   // "my-product" exists
            .ReturnsAsync(true)   // "my-product-1" exists
            .ReturnsAsync(false); // "my-product-2" is free

        // Act
        var slug = await _sut.GenerateSlugAsync("My Product");

        // Assert
        slug.Should().Be("my-product-2");
    }

    // ─── GetFeaturedProductsAsync ────────────────────────────────────────
    [Fact]
    public async Task GetFeaturedProductsAsync_ReturnsCorrectCount()
    {
        // Arrange
        var featured = TestDataBuilder.ProductFaker.Generate(4);
        featured.ForEach(p => { p.IsFeatured = true; p.Category = TestDataBuilder.CategoryFaker.Generate(); });
        _productRepoMock.Setup(r => r.GetFeaturedAsync(4)).ReturnsAsync(featured);

        // Act
        var result = await _sut.GetFeaturedProductsAsync(4);

        // Assert
        result.Should().HaveCount(4);
    }

    // ─── InStock property ────────────────────────────────────────────────
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    public async Task ProductListDto_InStock_ReflectsStockQuantity(int qty, bool expected)
    {
        // Arrange
        var product = TestDataBuilder.ProductFaker.Generate();
        product.StockQuantity = qty;
        product.Category = TestDataBuilder.CategoryFaker.Generate();
        _productRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<bool?>(), It.IsAny<string?>()))
            .ReturnsAsync((new List<Product> { product }, 1));

        // Act
        var result = await _sut.GetProductsAsync(new ProductFilterDto());

        // Assert
        result.Items.First().InStock.Should().Be(expected);
    }
}
