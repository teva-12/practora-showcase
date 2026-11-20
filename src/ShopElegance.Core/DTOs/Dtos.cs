using ShopElegance.Core.Enums;

namespace ShopElegance.Core.DTOs;

// --- Product DTOs ---
public class ProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SalesCount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool InStock => StockQuantity > 0;
}

public class ProductDetailDto : ProductListDto
{
    public string Description { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public double? Weight { get; set; }
    public string? Dimensions { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int ViewCount { get; set; }
    public int CategoryId { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool TrackInventory { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public string ProductType { get; set; } = "Physical";
    public string? DownloadUrl { get; set; }
    public string? SKU { get; set; }
    public double? Weight { get; set; }
    public string? Dimensions { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int CategoryId { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
}

public class UpdateProductDto : CreateProductDto
{
    public int Id { get; set; }
}

public class ProductFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; } = "newest";
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public string? ProductType { get; set; }
}

// --- Category DTOs ---
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int ProductCount { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class UpdateCategoryDto : CreateCategoryDto
{
    public int Id { get; set; }
}

// --- Cart DTOs ---
public class CartDto
{
    public int Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal => Items.Sum(i => i.TotalPrice);
    public int ItemCount => Items.Sum(i => i.Quantity);
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsDigital { get; set; }

    // White Coat customization (null for regular products)
    public int? WhiteCoatVariantId { get; set; }
    public CustomizationDto? Customization { get; set; }
}

// --- Order DTOs ---
public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentTransactionId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public string? Notes { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public ShippingAddressDto ShippingAddress { get; set; } = new();
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class OrderListDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsDigital { get; set; }
    public string? DownloadUrl { get; set; }

    // White Coat customization (null for regular products)
    public int? WhiteCoatVariantId { get; set; }
    public CustomizationDto? Customization { get; set; }
}

public class ShippingAddressDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class OrderStatusHistoryDto
{
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOrderDto
{
    public string UserId { get; set; } = string.Empty;
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public ShippingAddressDto ShippingAddress { get; set; } = new();
    public string? CouponCode { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // White Coat customization, carried over from the cart item (null for regular products)
    public int? WhiteCoatVariantId { get; set; }
    public CreateCustomizationDto? Customization { get; set; }
}

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? Comment { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ChangedByUserId { get; set; }
}

public class OrderFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// --- Wishlist DTOs ---
public class WishlistDto
{
    public int Id { get; set; }
    public List<WishlistItemDto> Items { get; set; } = new();
    public int Count => Items.Count;
}

public class WishlistItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public DateTime AddedAt { get; set; }
}

// --- Review DTOs ---
public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulVotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

// --- Coupon DTOs ---
public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Percentage";
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateCouponDto : CreateCouponDto
{
    public int Id { get; set; }
}

public class CouponValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public CouponDto? Coupon { get; set; }
}

// --- Dashboard DTOs ---
public class DashboardStatsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersGrowthCount { get; set; }
    public int TotalCustomers { get; set; }
    public int CustomersGrowthCount { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public List<ProductListDto> TopProducts { get; set; } = new();
    public List<OrderListDto> RecentOrders { get; set; } = new();
}

// --- Paged Result ---
public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
