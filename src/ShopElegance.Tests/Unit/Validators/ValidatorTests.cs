using FluentAssertions;
using FluentValidation.TestHelper;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Validators;
using Xunit;

namespace ShopElegance.Tests.Unit.Validators;

public class ProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void ValidProduct_PassesAllRules()
    {
        var dto = new CreateProductDto
        {
            Name = "Sony WH-1000XM5",
            ShortDescription = "Industry-leading noise cancelling headphones",
            Description = "The Sony WH-1000XM5 delivers exceptional audio quality with industry-leading noise cancellation technology.",
            Price = 349.99m,
            CompareAtPrice = 399.99m,
            StockQuantity = 25,
            CategoryId = 1,
            ProductType = "Physical",
            IsActive = true
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var dto = new CreateProductDto { Name = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_FailsValidation()
    {
        var dto = new CreateProductDto { Name = new string('A', 201) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void PriceZero_FailsValidation()
    {
        var dto = new CreateProductDto { Price = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void NegativePrice_FailsValidation()
    {
        var dto = new CreateProductDto { Price = -10 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void CompareAtPriceLowerThanPrice_FailsValidation()
    {
        var dto = new CreateProductDto { Price = 100m, CompareAtPrice = 50m };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CompareAtPrice);
    }

    [Fact]
    public void NegativeStock_FailsValidation()
    {
        var dto = new CreateProductDto { StockQuantity = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }

    [Fact]
    public void ZeroCategoryId_FailsValidation()
    {
        var dto = new CreateProductDto { CategoryId = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void DigitalProductWithoutDownloadUrl_FailsValidation()
    {
        var dto = new CreateProductDto { ProductType = "Digital", DownloadUrl = null };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.DownloadUrl);
    }

    [Fact]
    public void DigitalProductWithDownloadUrl_PassesValidation()
    {
        var dto = new CreateProductDto
        {
            Name = "Course",
            ShortDescription = "Great course for learning.",
            Description = "This is a comprehensive online course covering all topics in detail.",
            Price = 29.99m,
            StockQuantity = 9999,
            CategoryId = 1,
            ProductType = "Digital",
            DownloadUrl = "/downloads/course.zip",
            IsActive = true
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.DownloadUrl);
    }

    [Fact]
    public void InvalidProductType_FailsValidation()
    {
        var dto = new CreateProductDto { ProductType = "InvalidType" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductType);
    }
}

public class CouponValidatorTests
{
    private readonly CreateCouponValidator _validator = new();

    [Fact]
    public void ValidPercentageCoupon_PassesAllRules()
    {
        var dto = new CreateCouponDto
        {
            Code = "SUMMER25",
            Type = "Percentage",
            Value = 25,
            IsActive = true
        };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LowercaseCode_FailsValidation()
    {
        var dto = new CreateCouponDto { Code = "lowercase", Type = "Percentage", Value = 10, IsActive = true };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void PercentageOver100_FailsValidation()
    {
        var dto = new CreateCouponDto { Code = "BIG", Type = "Percentage", Value = 150, IsActive = true };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void ExpiredDate_FailsValidation()
    {
        var dto = new CreateCouponDto
        {
            Code = "OLD", Type = "Percentage", Value = 10,
            IsActive = true, ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void NegativeUsageLimit_FailsValidation()
    {
        var dto = new CreateCouponDto { Code = "BAD", Type = "FixedAmount", Value = 10, UsageLimit = -1 };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.UsageLimit);
    }
}

public class OrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    private static ShippingAddressDto ValidAddress() => new()
    {
        FirstName = "John", LastName = "Doe", Email = "john@test.com",
        Phone = "555-1234", AddressLine1 = "123 Main St",
        City = "New York", State = "NY", PostalCode = "10001", Country = "United States"
    };

    [Fact]
    public void ValidOrder_PassesAllRules()
    {
        var dto = new CreateOrderDto
        {
            UserId = "user123",
            ShippingAddress = ValidAddress(),
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 2, UnitPrice = 29.99m }
            }
        };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyItems_FailsValidation()
    {
        var dto = new CreateOrderDto
        {
            UserId = "user123",
            ShippingAddress = ValidAddress(),
            Items = new List<CreateOrderItemDto>()
        };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void EmptyUserId_FailsValidation()
    {
        var dto = new CreateOrderDto { UserId = "" };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void InvalidEmail_FailsAddressValidation()
    {
        var dto = new CreateOrderDto
        {
            UserId = "u1",
            ShippingAddress = new ShippingAddressDto
            {
                FirstName = "John", LastName = "Doe", Email = "not-an-email",
                Phone = "555", AddressLine1 = "123 St", City = "NYC",
                State = "NY", PostalCode = "10001", Country = "US"
            },
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1, UnitPrice = 10m } }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("ShippingAddress.Email");
    }
}

public class ReviewValidatorTests
{
    private readonly CreateReviewValidator _validator = new();

    [Fact]
    public void ValidReview_PassesAllRules()
    {
        var dto = new CreateReviewDto
        {
            ProductId = 1, Rating = 5,
            Title = "Amazing product!",
            Body = "I have been using this product for a month and it exceeded all my expectations."
        };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void InvalidRating_FailsValidation(int rating)
    {
        var dto = new CreateReviewDto { ProductId = 1, Rating = rating, Title = "T", Body = "Body text that is long enough" };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void ShortBody_FailsValidation()
    {
        var dto = new CreateReviewDto { ProductId = 1, Rating = 4, Title = "Ok", Body = "Too short." };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Body);
    }
}
