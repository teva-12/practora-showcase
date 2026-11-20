using FluentAssertions;
using Moq;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Core.Interfaces.Services;
using ShopElegance.Infrastructure.Services;
using Xunit;

namespace ShopElegance.Tests.Unit.Services;

public class CustomizationServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICustomizationRepository> _customizationRepoMock;
    private readonly Mock<IShippingCustomizationRepository> _shippingCustomizationRepoMock;
    private readonly Mock<IPriceCalculationService> _priceServiceMock;
    private readonly CustomizationService _sut;

    public CustomizationServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _customizationRepoMock = new Mock<ICustomizationRepository>();
        _shippingCustomizationRepoMock = new Mock<IShippingCustomizationRepository>();
        _priceServiceMock = new Mock<IPriceCalculationService>();
        _uowMock.Setup(u => u.Customizations).Returns(_customizationRepoMock.Object);
        _uowMock.Setup(u => u.ShippingCustomizations).Returns(_shippingCustomizationRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new CustomizationService(_uowMock.Object, _priceServiceMock.Object);
    }

    [Fact]
    public async Task CreateCustomizationAsync_PersistsFeeFromPriceService()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 5,
            EmbroideryEnabled = true,
            StudentName = "Mona",
            EmbroideryFontId = 2,
            EmbroideryPosition = "LeftChest"
        };
        _priceServiceMock.Setup(p => p.GetCustomizationCostsAsync(dto)).ReturnsAsync((100m, 0m));

        Customization? captured = null;
        _customizationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customization>()))
            .Callback<Customization>(c => { captured = c; c.Id = 42; })
            .Returns(Task.CompletedTask);

        var id = await _sut.CreateCustomizationAsync(dto);

        id.Should().Be(42);
        captured.Should().NotBeNull();
        captured!.PriceAdjustment.Should().Be(100m);
        captured.EmbroideryCost.Should().Be(100m);
        captured.LogoCost.Should().Be(0m);
        captured.WhiteCoatVariantId.Should().Be(5);
        captured.EmbroideryPosition.Should().Be(ShopElegance.Core.Enums.EmbroideryPosition.LeftChest);
    }

    [Fact]
    public async Task DeleteCustomizationAsync_UnknownId_DoesNotThrow()
    {
        _customizationRepoMock.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((Customization?)null);

        var act = async () => await _sut.DeleteCustomizationAsync(123);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCustomization_MapsFieldsCorrectly()
    {
        var customization = new Customization
        {
            Id = 7,
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Layla",
            PriceAdjustment = 6.50m
        };
        _customizationRepoMock.Setup(r => r.GetWithDetailsAsync(7)).ReturnsAsync(customization);

        var result = await _sut.GetByIdAsync(7);

        result.Should().NotBeNull();
        result!.StudentName.Should().Be("Layla");
        result.PriceAdjustment.Should().Be(6.50m);
    }
}
