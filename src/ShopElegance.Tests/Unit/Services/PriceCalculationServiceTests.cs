using FluentAssertions;
using Moq;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class PriceCalculationServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IWhiteCoatVariantRepository> _variantRepoMock;
    private readonly PriceCalculationService _sut;

    public PriceCalculationServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _variantRepoMock = new Mock<IWhiteCoatVariantRepository>();
        _uowMock.Setup(u => u.WhiteCoatVariants).Returns(_variantRepoMock.Object);
        _sut = new PriceCalculationService(_uowMock.Object);
    }

    [Fact]
    public async Task CalculateCustomizationFeeAsync_NoCustomization_ReturnsZero()
    {
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1, EmbroideryEnabled = false };

        var fee = await _sut.CalculateCustomizationFeeAsync(dto);

        fee.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateCustomizationFeeAsync_EmbroideryOnly_ReturnsHundredFifty()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Omar"
        };

        var fee = await _sut.CalculateCustomizationFeeAsync(dto);

        fee.Should().Be(150m);
    }

    [Fact]
    public async Task CalculateCustomizationFeeAsync_LogoOnly_ReturnsHundredFifty()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            LogoType = "University",
            LogoId = 1
        };

        var fee = await _sut.CalculateCustomizationFeeAsync(dto);

        fee.Should().Be(150m);
    }

    [Fact]
    public async Task CalculateCustomizationFeeAsync_EmbroideryAndLogo_ReturnsHundredFifty()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Omar",
            LogoType = "University",
            LogoId = 1
        };

        var fee = await _sut.CalculateCustomizationFeeAsync(dto);

        fee.Should().Be(150m);
    }

    [Fact]
    public async Task CalculateCustomizationFeeAsync_CustomLogo_ReturnsHundredFifty()
    {
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1, LogoType = "Custom", CustomLogoUrl = "url" };

        var fee = await _sut.CalculateCustomizationFeeAsync(dto);

        fee.Should().Be(150m);
    }

    [Fact]
    public async Task CalculateVariantFinalPriceAsync_AddsVariantAdjustmentToProductPrice()
    {
        var variant = new WhiteCoatVariant
        {
            Id = 1,
            PriceAdjustment = 15m,
            Product = new Product { Id = 1, Price = 100m }
        };
        _variantRepoMock.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(variant);

        var price = await _sut.CalculateVariantFinalPriceAsync(1);

        price.Should().Be(115m);
    }

    [Fact]
    public async Task CalculateVariantFinalPriceAsync_UnknownVariant_ThrowsKeyNotFound()
    {
        _variantRepoMock.Setup(r => r.GetWithDetailsAsync(999)).ReturnsAsync((WhiteCoatVariant?)null);

        var act = async () => await _sut.CalculateVariantFinalPriceAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
