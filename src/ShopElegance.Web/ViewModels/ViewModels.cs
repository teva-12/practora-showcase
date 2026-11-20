using System.ComponentModel.DataAnnotations;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Enums;

namespace ShopElegance.Web.ViewModels;

// --- Account ---
public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
        ErrorMessage = "Password must include an uppercase letter, a lowercase letter, and a number.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
        ErrorMessage = "Password must include an uppercase letter, a lowercase letter, and a number.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must include an uppercase letter, a lowercase letter, and a number.")]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    public string? AvatarUrl { get; set; }
}

// --- Checkout ---
public class CheckoutViewModel
{
    public CartDto Cart { get; set; } = new();
    public CheckoutAddressViewModel ShippingAddress { get; set; } = new();
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxRate { get; set; } = 0m;
    public decimal TaxAmount => (Cart.SubTotal - DiscountAmount + ShippingCost) * TaxRate;
    public decimal OrderTotal => Cart.SubTotal - DiscountAmount + ShippingCost + TaxAmount;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
    public IReadOnlyList<string> Governorates { get; set; } = Array.Empty<string>();
    public int EstimatedDeliveryDays { get; set; } = 3;
    public List<CheckoutCustomizationEditViewModel> Customizations { get; set; } = new();
}

/// <summary>Lets the customer fix a typo or tweak embroidery text in the review step, before confirming.</summary>
public class CheckoutCustomizationEditViewModel
{
    public int CartItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantSummary { get; set; } = string.Empty;
    [StringLength(150)]
    public string? StudentName { get; set; }
    [StringLength(150)]
    public string? DoctorName { get; set; }
    [StringLength(150)]
    public string? Faculty { get; set; }
    [StringLength(150)]
    public string? Department { get; set; }
    public int? GraduationYear { get; set; }
    public string? EmbroideryTextColor { get; set; }
    public string? CustomLogoUrl { get; set; }
}

public class CheckoutAddressViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(300)]
    [Display(Name = "Address")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Apartment, suite, etc.")]
    public string? AddressLine2 { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Governorate")]
    public string State { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Postal Code (optional)")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = "Egypt";
}

// --- Shop ---
public class ShopViewModel
{
    public PagedResultDto<ProductListDto> Products { get; set; } = new();
    public IEnumerable<CategoryDto> Categories { get; set; } = Enumerable.Empty<CategoryDto>();
    public ProductFilterDto Filter { get; set; } = new();
    public string? CurrentCategoryName { get; set; }
    public decimal MaxPriceInCatalog { get; set; } = 1000;
}

public class ProductDetailViewModel
{
    public ProductDetailDto Product { get; set; } = null!;
    public IEnumerable<ProductListDto> RelatedProducts { get; set; } = Enumerable.Empty<ProductListDto>();
    public PagedResultDto<ReviewDto> Reviews { get; set; } = new();
    public bool IsInWishlist { get; set; }
    public bool UserHasReviewed { get; set; }
    public bool CanReview { get; set; }
    public CreateReviewViewModel? NewReview { get; set; }
    public bool HasWhiteCoatVariants { get; set; }
}

public class WhiteCoatCustomizeViewModel
{
    public ProductDetailDto Product { get; set; } = null!;
    public WhiteCoatCustomerOptionsDto Options { get; set; } = null!;
}

public class CreateReviewViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required, Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 20)]
    public string Body { get; set; } = string.Empty;
}

// --- Admin Product ---
public class AdminProductViewModel
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Short Description")]
    [StringLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Display(Name = "Compare At Price")]
    [Range(0.01, double.MaxValue)]
    public decimal? CompareAtPrice { get; set; }

    [Required, Range(0, int.MaxValue)]
    [Display(Name = "Stock Quantity")]
    public int StockQuantity { get; set; }

    [Display(Name = "Track Inventory")]
    public bool TrackInventory { get; set; } = true;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Featured")]
    public bool IsFeatured { get; set; }

    [Required]
    [Display(Name = "Product Type")]
    public string ProductType { get; set; } = "Physical";

    [Display(Name = "Download URL")]
    public string? DownloadUrl { get; set; }

    [StringLength(100)]
    public string? SKU { get; set; }

    [Range(0, double.MaxValue)]
    public double? Weight { get; set; }

    [StringLength(100)]
    public string? Dimensions { get; set; }

    [StringLength(200)]
    [Display(Name = "Meta Title")]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    [Display(Name = "Meta Description")]
    public string? MetaDescription { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    public string TagsInput { get; set; } = string.Empty;
    public List<IFormFile> Images { get; set; } = new();
    public IEnumerable<CategoryDto> Categories { get; set; } = Enumerable.Empty<CategoryDto>();
}

// --- Address ---
public class AddressViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(300)]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "State / Province")]
    public string State { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Postal Code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = "United States";

    [Display(Name = "Set as default")]
    public bool IsDefault { get; set; }
}
