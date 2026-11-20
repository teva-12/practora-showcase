using FluentAssertions;
using Moq;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class CustomizationPreviewServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IWhiteCoatVariantRepository> _variantRepoMock;
    private readonly Mock<ILogoRepository> _logoRepoMock;
    private readonly Mock<IPriceCalculationService> _priceServiceMock;
    private readonly CustomizationPreviewService _sut;

    public CustomizationPreviewServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _variantRepoMock = new Mock<IWhiteCoatVariantRepository>();
        _logoRepoMock = new Mock<ILogoRepository>();
        _priceServiceMock = new Mock<IPriceCalculationService>();
        _uowMock.Setup(u => u.WhiteCoatVariants).Returns(_variantRepoMock.Object);
        _uowMock.Setup(u => u.Logos).Returns(_logoRepoMock.Object);
        _sut = new CustomizationPreviewService(_uowMock.Object, _priceServiceMock.Object);
    }

    private static WhiteCoatVariant BuildVariant(decimal priceAdjustment = 10m, decimal basePrice = 100m)
    {
        var variant = new WhiteCoatVariant
        {
            Id = 1,
            PriceAdjustment = priceAdjustment,
            Product = new Product { Id = 1, Price = basePrice }
        };
        variant.Images.Add(new WhiteCoatVariantImage { Url = "/images/whitecoats/primary.jpg", IsPrimary = true });
        return variant;
    }

    [Fact]
    public async Task GeneratePreviewAsync_UnknownVariant_ThrowsKeyNotFound()
    {
        _variantRepoMock.Setup(r => r.GetWithDetailsAsync(999)).ReturnsAsync((WhiteCoatVariant?)null);

        var request = new WhiteCoatPreviewRequestDto { WhiteCoatVariantId = 999 };
        var act = async () => await _sut.GeneratePreviewAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GeneratePreviewAsync_ReturnsBasePriceImageAndFee()
    {
        var variant = BuildVariant();
        _variantRepoMock.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(variant);
        _priceServiceMock
            .Setup(p => p.GetCustomizationCostsAsync(It.IsAny<CreateCustomizationDto>()))
            .ReturnsAsync((100m, 0m));

        var request = new WhiteCoatPreviewRequestDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Nour"
        };

        var result = await _sut.GeneratePreviewAsync(request);

        result.BaseImageUrl.Should().Be("/images/whitecoats/primary.jpg");
        result.BasePrice.Should().Be(110m); // 100 + 10 adjustment
        result.CustomizationFee.Should().Be(100m);
        result.FinalPrice.Should().Be(210m);
    }

    [Fact]
    public async Task GeneratePreviewAsync_CatalogLogo_ResolvesLogoImageUrl()
    {
        var variant = BuildVariant();
        _variantRepoMock.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(variant);
        _logoRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Logo { Id = 3, ImageUrl = "/images/logos/cairo-uni.png" });
        _priceServiceMock.Setup(p => p.GetCustomizationCostsAsync(It.IsAny<CreateCustomizationDto>())).ReturnsAsync((0m, 100m));

        var request = new WhiteCoatPreviewRequestDto
        {
            WhiteCoatVariantId = 1,
            LogoType = "University",
            LogoId = 3
        };

        var result = await _sut.GeneratePreviewAsync(request);

        result.LogoImageUrl.Should().Be("/images/logos/cairo-uni.png");
    }

    [Fact]
    public async Task GeneratePreviewAsync_CustomLogo_UsesProvidedUrlDirectly()
    {
        var variant = BuildVariant();
        _variantRepoMock.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(variant);
        _priceServiceMock.Setup(p => p.GetCustomizationCostsAsync(It.IsAny<CreateCustomizationDto>())).ReturnsAsync((0m, 100m));

        var request = new WhiteCoatPreviewRequestDto
        {
            WhiteCoatVariantId = 1,
            LogoType = "Custom",
            CustomLogoUrl = "/images/customer-logos/xyz.png"
        };

        var result = await _sut.GeneratePreviewAsync(request);

        result.LogoImageUrl.Should().Be("/images/customer-logos/xyz.png");
        _logoRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
}
