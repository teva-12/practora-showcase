using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Http;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class CartServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<IPriceCalculationService> _priceServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly CartService _sut;

    public CartServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _cartRepoMock = new Mock<ICartRepository>();
        _priceServiceMock = new Mock<IPriceCalculationService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        // Setup a mock context with a functional mock session that prevents NREs
        var sessionMock = new Mock<ISession>();
        var httpContextMock = new Mock<HttpContext>();
        
        // Ensure Session property is not null
        httpContextMock.Setup(h => h.Session).Returns(sessionMock.Object);
        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContextMock.Object);
        
        _uowMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new CartService(_uowMock.Object, _priceServiceMock.Object, _httpContextAccessorMock.Object);
    }

    private static Product MakeProduct(int id = 1, decimal price = 29.99m, int stock = 10) => new()
    {
        Id = id, Name = $"Product {id}", Slug = $"product-{id}",
        Price = price, StockQuantity = stock,
        Type = ProductType.Physical, IsActive = true,
        Description = "desc", ShortDescription = "short",
        Images = new List<ProductImage>()
    };

    private static Cart MakeCart(string? userId = "user1", string? sessionId = null)
    {
        var cart = new Cart { Id = 1, UserId = userId, SessionId = sessionId };
        cart.Items = new List<CartItem>();
        return cart;
    }

    // ─── GetCartAsync ────────────────────────────────────────────────────
    [Fact]
    public async Task GetCartAsync_UserHasCart_ReturnsCartDto()
    {
        var product = MakeProduct();
        var cart = MakeCart("user1");
        cart.Items.Add(new CartItem { Id = 1, CartId = 1, ProductId = 1, Product = product, Quantity = 2 });

        _cartRepoMock.Setup(r => r.GetWithItemsByUserIdAsync("user1")).ReturnsAsync(cart);

        var result = await _sut.GetCartAsync("user1", null);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.ItemCount.Should().Be(2);
        result.SubTotal.Should().Be(59.98m);
    }

    [Fact]
    public async Task GetCartAsync_NoCart_ReturnsEmptyCart()
    {
        _cartRepoMock.Setup(r => r.GetWithItemsByUserIdAsync(It.IsAny<string>())).ReturnsAsync((Cart?)null);

        var result = await _sut.GetCartAsync("user1", null);

        result.Items.Should().BeEmpty();
        result.ItemCount.Should().Be(0);
        result.SubTotal.Should().Be(0);
    }

    // ─── AddToCartAsync ──────────────────────────────────────────────────
    [Fact]
    public async Task AddToCartAsync_NewItem_AddsItemToCart()
    {
        var product = MakeProduct();
        var cart = MakeCart("user1");
        cart.Items = new List<CartItem>(); // Explicitly initialize

        _cartRepoMock.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(cart);
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(cart);

        var result = await _sut.AddToCartAsync("user1", null, 1, 3);

        result.Items.Should().HaveCount(1);
        result.Items.First().Quantity.Should().Be(3);
    }

    [Fact]
    public async Task AddToCartAsync_ExistingItem_IncreasesQuantity()
    {
        var product = MakeProduct();
        var existingItem = new CartItem { Id = 1, CartId = 1, ProductId = 1, Product = product, Quantity = 2 };
        var cart = MakeCart("user1");
        cart.Items.Add(existingItem);

        _cartRepoMock.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(cart);
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(cart);

        await _sut.AddToCartAsync("user1", null, 1, 3);

        existingItem.Quantity.Should().Be(5);
    }

    // ─── RemoveFromCartAsync ─────────────────────────────────────────────
    [Fact]
    public async Task RemoveFromCartAsync_ExistingItem_RemovesItem()
    {
        var product = MakeProduct();
        var item = new CartItem { Id = 1, CartId = 1, ProductId = 1, Product = product, Quantity = 2 };
        var cart = MakeCart("user1");
        cart.Items.Add(item);

        _cartRepoMock.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(cart);
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(cart);

        await _sut.RemoveFromCartAsync("user1", null, 1);

        cart.Items.Should().BeEmpty();
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    // ─── GetCartItemCountAsync ───────────────────────────────────────────
    [Fact]
    public async Task GetCartItemCountAsync_WithItems_ReturnsTotalQuantity()
    {
        var product = MakeProduct();
        var cart = MakeCart("user1");
        cart.Items.Add(new CartItem { Id = 1, ProductId = 1, Product = product, Quantity = 3 });
        cart.Items.Add(new CartItem { Id = 2, ProductId = 2, Product = MakeProduct(2), Quantity = 2 });

        _cartRepoMock.Setup(r => r.GetWithItemsByUserIdAsync("user1")).ReturnsAsync(cart);

        var count = await _sut.GetCartItemCountAsync("user1", null);

        count.Should().Be(5);
    }

    // ─── ClearCartAsync ──────────────────────────────────────────────────
    [Fact]
    public async Task ClearCartAsync_CartWithItems_ClearsAllItems()
    {
        var product = MakeProduct();
        var cart = MakeCart("user1");
        cart.Items.Add(new CartItem { Id = 1, ProductId = 1, Product = product, Quantity = 2 });

        _cartRepoMock.Setup(r => r.GetWithItemsByUserIdAsync("user1")).ReturnsAsync(cart);

        await _sut.ClearCartAsync("user1", null);

        cart.Items.Should().BeEmpty();
    }

    // ─── MergeCartsAsync ─────────────────────────────────────────────────
    [Fact]
    public async Task MergeCartsAsync_UserHasNoCart_AssignsSessionCartToUser()
    {
        var product = MakeProduct();
        var sessionCart = new Cart { Id = 2, SessionId = "sess123" };
        sessionCart.Items.Add(new CartItem { Id = 1, ProductId = 1, Product = product, Quantity = 1 });

        _cartRepoMock.Setup(r => r.GetWithItemsByUserIdAsync("user1")).ReturnsAsync((Cart?)null);
        _cartRepoMock.Setup(r => r.GetWithItemsBySessionIdAsync("sess123")).ReturnsAsync(sessionCart);

        await _sut.MergeCartsAsync("user1", "sess123");

        sessionCart.UserId.Should().Be("user1");
        sessionCart.SessionId.Should().BeNull();
    }
}
