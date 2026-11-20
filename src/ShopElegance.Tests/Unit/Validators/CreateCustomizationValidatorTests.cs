using FluentAssertions;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Validators;
using Xunit;

namespace ShopElegance.Tests.Unit.Validators;

public class CreateCustomizationValidatorTests
{
    private readonly CreateCustomizationValidator _sut = new();

    [Fact]
    public void NoEmbroideryNoLogo_IsValid()
    {
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1 };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void VariantIdZero_IsValid()
    {
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 0 };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmbroideryEnabledWithoutAnyName_FailsValidation()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            EmbroideryFontId = 1,
            EmbroideryPosition = "LeftChest"
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmbroideryEnabledWithoutFont_FailsValidation()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Omar",
            EmbroideryPosition = "LeftChest"
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmbroideryFontId");
    }

    [Fact]
    public void EmbroideryEnabledWithAllRequiredFields_IsValid()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Omar",
            EmbroideryFontId = 2,
            EmbroideryPosition = "LeftChest",
            EmbroideryTextColor = "#1A1A1A"
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidHexColor_FailsValidation()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Omar",
            EmbroideryFontId = 2,
            EmbroideryPosition = "LeftChest",
            EmbroideryTextColor = "not-a-color"
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmbroideryTextColor");
    }

    [Fact]
    public void CatalogLogoTypeWithoutLogoId_FailsValidation()
    {
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1, LogoType = "University" };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LogoId");
    }

    [Fact]
    public void HospitalLogoTypeWithoutLogoId_FailsValidation()
    {
        // Same rule as University — LogoId is required for both catalog-backed logo types.
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1, LogoType = "Hospital" };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LogoId");
    }

    [Fact]
    public void CatalogLogoTypeWithZeroLogoId_FailsValidation()
    {
        // LogoId = 0 is a distinct case from null (e.g. an unbound/unselected dropdown posting 0) —
        // must be rejected the same as a missing LogoId.
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1, LogoType = "University", LogoId = 0 };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LogoId");
    }

    [Fact]
    public void CatalogLogoTypeWithValidLogoId_IsValid()
    {
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1, LogoType = "University", LogoId = 5 };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmbroideryEnabledWithZeroFontId_FailsValidation()
    {
        // EmbroideryFontId = 0 is a distinct case from null — must be rejected the same way.
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            EmbroideryEnabled = true,
            StudentName = "Omar",
            EmbroideryFontId = 0,
            EmbroideryPosition = "LeftChest"
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmbroideryFontId");
    }

    [Fact]
    public void CustomLogoTypeWithoutUploadUrl_FailsValidation()
    {
        var dto = new CreateCustomizationDto { WhiteCoatVariantId = 1, LogoType = "Custom" };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomLogoUrl");
    }

    [Fact]
    public void CustomLogoTypeWithUploadUrl_IsValid()
    {
        var dto = new CreateCustomizationDto
        {
            WhiteCoatVariantId = 1,
            LogoType = "Custom",
            CustomLogoUrl = "/images/customer-logos/abc123.png"
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }
}

public class AddCustomizedItemToCartValidatorTests
{
    private readonly AddCustomizedItemToCartValidator _sut = new();

    [Fact]
    public void ValidRequest_IsValid()
    {
        var dto = new AddCustomizedItemToCartDto
        {
            ProductId = 1,
            Quantity = 2,
            Customization = new CreateCustomizationDto { WhiteCoatVariantId = 1 }
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroQuantity_FailsValidation()
    {
        var dto = new AddCustomizedItemToCartDto
        {
            ProductId = 1,
            Quantity = 0,
            Customization = new CreateCustomizationDto { WhiteCoatVariantId = 1 }
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ExcessiveQuantity_FailsValidation()
    {
        var dto = new AddCustomizedItemToCartDto
        {
            ProductId = 1,
            Quantity = 101,
            Customization = new CreateCustomizationDto { WhiteCoatVariantId = 1 }
        };

        var result = _sut.Validate(dto);

        result.IsValid.Should().BeFalse();
    }
}
