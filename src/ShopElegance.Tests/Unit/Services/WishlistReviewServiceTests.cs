using FluentAssertions;
using Moq;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

// ────────────────────────────────────────────────────────────────────────────
//  WishlistService Tests
// ────────────────────────────────────────────────────────────────────────────
public class WishlistServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IWishlistRepository> _wishlistRepoMock;
    private readonly WishlistService _sut;

    public WishlistServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _wishlistRepoMock = new Mock<IWishlistRepository>();
        _uowMock.Setup(u => u.Wishlists).Returns(_wishlistRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new WishlistService(_uowMock.Object);
    }

    [Fact]
    public async Task GetWishlistAsync_UserHasWishlist_ReturnsDto()
    {
        var product = new Product
        {
            Id = 1, Name = "Nice Watch", Slug = "nice-watch",
            Price = 199.99m, StockQuantity = 5,
            Description = "d", ShortDescription = "s",
            Images = new List<ProductImage>()
        };
        var wishlist = new Wishlist
        {
            Id = 1, UserId = "user1",
            Items = new List<WishlistItem>
            {
                new() { Id = 1, WishlistId = 1, ProductId = 1, Product = product, CreatedAt = DateTime.UtcNow }
            }
        };
        _wishlistRepoMock.Setup(r => r.GetWithItemsAsync("user1")).ReturnsAsync(wishlist);

        var result = await _sut.GetWishlistAsync("user1");

        result.Count.Should().Be(1);
        result.Items.First().ProductName.Should().Be("Nice Watch");
        result.Items.First().Price.Should().Be(199.99m);
        result.Items.First().InStock.Should().BeTrue();
    }

    [Fact]
    public async Task GetWishlistAsync_NoWishlist_ReturnsEmptyDto()
    {
        _wishlistRepoMock.Setup(r => r.GetWithItemsAsync(It.IsAny<string>())).ReturnsAsync((Wishlist?)null);

        var result = await _sut.GetWishlistAsync("user1");

        result.Items.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task AddToWishlistAsync_NewProduct_AddsSuccessfully()
    {
        _wishlistRepoMock.Setup(r => r.HasProductAsync("user1", 5)).ReturnsAsync(false);
        _wishlistRepoMock.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new Wishlist { Id = 1, UserId = "user1", Items = new List<WishlistItem>() });

        var result = await _sut.AddToWishlistAsync("user1", 5);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddToWishlistAsync_AlreadyInWishlist_ReturnsFalse()
    {
        _wishlistRepoMock.Setup(r => r.HasProductAsync("user1", 5)).ReturnsAsync(true);

        var result = await _sut.AddToWishlistAsync("user1", 5);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_ExistingItem_RemovesAndReturnsTrue()
    {
        var product = new Product { Id = 5, Name = "P", Slug = "p", Description = "d", ShortDescription = "s" };
        var wishlist = new Wishlist
        {
            Id = 1, UserId = "user1",
            Items = new List<WishlistItem>
            {
                new() { Id = 1, WishlistId = 1, ProductId = 5, Product = product }
            }
        };
        _wishlistRepoMock.Setup(r => r.GetWithItemsAsync("user1")).ReturnsAsync(wishlist);

        var result = await _sut.RemoveFromWishlistAsync("user1", 5);

        result.Should().BeTrue();
        wishlist.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task IsInWishlistAsync_ProductInWishlist_ReturnsTrue()
    {
        _wishlistRepoMock.Setup(r => r.HasProductAsync("user1", 3)).ReturnsAsync(true);

        var result = await _sut.IsInWishlistAsync("user1", 3);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetWishlistCountAsync_WithItems_ReturnsCount()
    {
        var product = new Product { Id = 1, Name = "P", Slug = "p", Description = "d", ShortDescription = "s", Images = new List<ProductImage>() };
        var wishlist = new Wishlist
        {
            Id = 1, UserId = "user1",
            Items = new List<WishlistItem>
            {
                new() { Id = 1, ProductId = 1, Product = product },
                new() { Id = 2, ProductId = 2, Product = product }
            }
        };
        _wishlistRepoMock.Setup(r => r.GetWithItemsAsync("user1")).ReturnsAsync(wishlist);

        var count = await _sut.GetWishlistCountAsync("user1");

        count.Should().Be(2);
    }
}

// ────────────────────────────────────────────────────────────────────────────
//  ReviewService Tests
// ────────────────────────────────────────────────────────────────────────────
public class ReviewServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IReviewRepository> _reviewRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly ReviewService _sut;

    public ReviewServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _reviewRepoMock = new Mock<IReviewRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _uowMock.Setup(u => u.Reviews).Returns(_reviewRepoMock.Object);
        _uowMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new ReviewService(_uowMock.Object);
    }

    [Fact]
    public async Task CreateReviewAsync_ValidReview_CreatesSuccessfully()
    {
        _reviewRepoMock.Setup(r => r.IsVerifiedPurchaseAsync("user1", 1)).ReturnsAsync(true);
        _reviewRepoMock.Setup(r => r.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);

        var dto = new Core.DTOs.CreateReviewDto
        {
            ProductId = 1, Rating = 5,
            Title = "Excellent!", Body = "Really loved this product, would buy again."
        };

        var result = await _sut.CreateReviewAsync("user1", dto);

        result.Should().NotBeNull();
        result.Rating.Should().Be(5);
        result.Title.Should().Be("Excellent!");
        result.IsVerifiedPurchase.Should().BeTrue();
        result.IsApproved.Should().BeFalse(); // pending moderation
    }

    [Fact]
    public async Task ApproveReviewAsync_ValidReview_SetsApprovedTrue()
    {
        var review = new Review { Id = 1, ProductId = 1, Rating = 4, Title = "Good", Body = "Nice.", IsApproved = false };
        var product = new Product
        {
            Id = 1, AverageRating = 0, ReviewCount = 0,
            Name = "P", Slug = "p", Description = "d", ShortDescription = "s"
        };
        _reviewRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);
        _reviewRepoMock.Setup(r => r.GetProductReviewsAsync(1, 1, 1000, true))
            .ReturnsAsync((new List<Review> { review }, 1));
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        await _sut.ApproveReviewAsync(1);

        review.IsApproved.Should().BeTrue();
        product.AverageRating.Should().Be(4);
        product.ReviewCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteReviewAsync_ExistingReview_SoftDeletes()
    {
        var review = new Review { Id = 1, IsDeleted = false };
        _reviewRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        await _sut.DeleteReviewAsync(1);

        review.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductReviewsAsync_ReturnsPagedReviews()
    {
        var reviews = new List<Review>
        {
            new() { Id=1, Rating=5, Title="Great", Body="Loved it", UserId="u1",
                    User=new ApplicationUser{FirstName="Jane",LastName="Smith"}, CreatedAt=DateTime.UtcNow }
        };
        _reviewRepoMock.Setup(r => r.GetProductReviewsAsync(1, 1, 5, true)).ReturnsAsync((reviews, 1));

        var result = await _sut.GetProductReviewsAsync(1, 1, 5);

        result.TotalCount.Should().Be(1);
        result.Items.First().Title.Should().Be("Great");
    }
}
