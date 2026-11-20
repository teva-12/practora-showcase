using FluentValidation;
using ShopElegance.Core.DTOs;

namespace ShopElegance.Core.Validators;

// ─── Product Validators ──────────────────────────────────────────────────────
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.ShortDescription)
            .NotEmpty().WithMessage("Short description is required.")
            .MaximumLength(500).WithMessage("Short description cannot exceed 500 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .When(x => x.CompareAtPrice.HasValue)
            .WithMessage("Compare-at price must be greater than the sale price.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("A valid category is required.");

        RuleFor(x => x.ProductType)
            .Must(t => new[] { "Physical", "Digital", "Both" }.Contains(t))
            .WithMessage("Product type must be Physical, Digital, or Both.");

        RuleFor(x => x.DownloadUrl)
            .NotEmpty().When(x => x.ProductType is "Digital" or "Both")
            .WithMessage("Download URL is required for digital products.");

        RuleFor(x => x.SKU)
            .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta title cannot exceed 200 characters.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta description cannot exceed 500 characters.");
    }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        Include(new CreateProductValidator());
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid product ID.");
    }
}

// ─── Category Validators ─────────────────────────────────────────────────────
public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order cannot be negative.");
    }
}

// ─── Order Validators ────────────────────────────────────────────────────────
public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item.")
            .Must(items => items.All(i => i.Quantity > 0))
            .WithMessage("All order items must have a quantity greater than zero.")
            .Must(items => items.All(i => i.UnitPrice > 0))
            .WithMessage("All order items must have a valid price.");

        RuleFor(x => x.ShippingAddress).SetValidator(new ShippingAddressValidator());
    }
}

public class ShippingAddressValidator : AbstractValidator<ShippingAddressDto>
{
    public ShippingAddressValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50);

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100);

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State/Province is required.")
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20)
            .Matches(@"^[a-zA-Z0-9\s\-]{3,10}$").WithMessage("Invalid postal code format.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);
    }
}

// ─── Review Validators ───────────────────────────────────────────────────────
public class CreateReviewValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Invalid product.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Review title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Review body is required.")
            .MinimumLength(20).WithMessage("Review must be at least 20 characters.")
            .MaximumLength(2000).WithMessage("Review cannot exceed 2000 characters.");
    }
}

// ─── Coupon Validators ───────────────────────────────────────────────────────
public class CreateCouponValidator : AbstractValidator<CreateCouponDto>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters.")
            .Matches(@"^[A-Z0-9]+$").When(x => !string.IsNullOrEmpty(x.Code))
            .WithMessage("Coupon code must contain only uppercase letters and numbers.");

        RuleFor(x => x.Type)
            .Must(t => new[] { "Percentage", "FixedAmount" }.Contains(t))
            .WithMessage("Coupon type must be Percentage or FixedAmount.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Discount value must be greater than zero.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.Type == "Percentage")
            .WithMessage("Percentage discount cannot exceed 100%.");

        RuleFor(x => x.MinimumOrderAmount)
            .GreaterThanOrEqualTo(0).When(x => x.MinimumOrderAmount.HasValue)
            .WithMessage("Minimum order amount cannot be negative.");

        RuleFor(x => x.MaximumDiscountAmount)
            .GreaterThan(0).When(x => x.MaximumDiscountAmount.HasValue)
            .WithMessage("Maximum discount amount must be greater than zero.");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiry date must be in the future.");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).When(x => x.UsageLimit.HasValue)
            .WithMessage("Usage limit must be greater than zero.");
    }
}
