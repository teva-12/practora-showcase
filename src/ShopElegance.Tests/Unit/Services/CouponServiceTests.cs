using FluentAssertions;
using Moq;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class CouponServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICouponRepository> _couponRepoMock;
    private readonly CouponService _sut;

    public CouponServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _couponRepoMock = new Mock<ICouponRepository>();
        _uowMock.Setup(u => u.Coupons).Returns(_couponRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new CouponService(_uowMock.Object);
    }

    // ─── ValidateCouponAsync ─────────────────────────────────────────────
    [Fact]
    public async Task ValidateCouponAsync_ValidPercentageCoupon_ReturnsCorrectDiscount()
    {
        var coupon = new Coupon
        {
            Id = 1, Code = "SAVE10", Type = CouponType.Percentage,
            Value = 10, IsActive = true, UsageCount = 0
        };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE10")).ReturnsAsync(coupon);

        var result = await _sut.ValidateCouponAsync("SAVE10", 200m);

        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(20m); // 10% of 200
    }

    [Fact]
    public async Task ValidateCouponAsync_ValidFixedCoupon_ReturnsFixedDiscount()
    {
        var coupon = new Coupon
        {
            Id = 1, Code = "SAVE20", Type = CouponType.FixedAmount,
            Value = 20, IsActive = true, UsageCount = 0
        };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("SAVE20")).ReturnsAsync(coupon);

        var result = await _sut.ValidateCouponAsync("SAVE20", 150m);

        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(20m);
    }

    [Fact]
    public async Task ValidateCouponAsync_InvalidCode_ReturnsInvalid()
    {
        _couponRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Coupon?)null);

        var result = await _sut.ValidateCouponAsync("FAKECODE", 100m);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task ValidateCouponAsync_ExpiredCoupon_ReturnsInvalid()
    {
        var coupon = new Coupon
        {
            Code = "EXPIRED", Type = CouponType.Percentage, Value = 10,
            IsActive = true, ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("EXPIRED")).ReturnsAsync(coupon);

        var result = await _sut.ValidateCouponAsync("EXPIRED", 100m);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidateCouponAsync_InactiveCoupon_ReturnsInvalid()
    {
        var coupon = new Coupon { Code = "INACTIVE", Type = CouponType.Percentage, Value = 10, IsActive = false };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("INACTIVE")).ReturnsAsync(coupon);

        var result = await _sut.ValidateCouponAsync("INACTIVE", 100m);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCouponAsync_UsageLimitReached_ReturnsInvalid()
    {
        var coupon = new Coupon
        {
            Code = "USED", Type = CouponType.Percentage, Value = 10,
            IsActive = true, UsageLimit = 5, UsageCount = 5
        };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("USED")).ReturnsAsync(coupon);

        var result = await _sut.ValidateCouponAsync("USED", 100m);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("usage limit");
    }

    [Fact]
    public async Task ValidateCouponAsync_BelowMinimum_ReturnsInvalid()
    {
        var coupon = new Coupon
        {
            Code = "MIN100", Type = CouponType.Percentage, Value = 10,
            IsActive = true, MinimumOrderAmount = 100m
        };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("MIN100")).ReturnsAsync(coupon);

        var result = await _sut.ValidateCouponAsync("MIN100", 50m);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Minimum");
    }

    [Fact]
    public async Task ValidateCouponAsync_PercentageWithMaxCap_CapsDiscount()
    {
        var coupon = new Coupon
        {
            Code = "CAPPED", Type = CouponType.Percentage, Value = 50,
            IsActive = true, MaximumDiscountAmount = 30m
        };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("CAPPED")).ReturnsAsync(coupon);

        var result = await _sut.ValidateCouponAsync("CAPPED", 200m);

        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(30m); // capped at 30, not 100
    }

    [Fact]
    public async Task IncrementUsageAsync_ExistingCoupon_IncrementsCount()
    {
        var coupon = new Coupon { Code = "INC", Type = CouponType.FixedAmount, Value = 10, UsageCount = 3 };
        _couponRepoMock.Setup(r => r.GetByCodeAsync("INC")).ReturnsAsync(coupon);

        await _sut.IncrementUsageAsync("INC");

        coupon.UsageCount.Should().Be(4);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
