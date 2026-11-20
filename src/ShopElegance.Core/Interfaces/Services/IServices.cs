using ShopElegance.Core.DTOs;

namespace ShopElegance.Core.Interfaces.Services;

public interface IProductService
{
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug);
    Task<ProductDetailDto?> GetProductByIdAsync(int id);
    Task<PagedResultDto<ProductListDto>> GetProductsAsync(ProductFilterDto filter);
    Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync(int count = 8);
    Task<IEnumerable<ProductListDto>> GetRelatedProductsAsync(int productId, int categoryId, int count = 4);
    Task<IEnumerable<ProductListDto>> GetBestSellersAsync(int count = 8);
    Task<IEnumerable<ProductListDto>> GetLowStockProductsAsync(int threshold = 5);
    Task<ProductDetailDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductDetailDto> UpdateProductAsync(int id, UpdateProductDto dto);
    Task DeleteProductAsync(int id);
    Task<bool> ProductExistsAsync(int id);
    Task<string> GenerateSlugAsync(string name);
    Task IncrementViewCountAsync(int productId);
}

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync();
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(int id);
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(string? userId, string? sessionId);
    Task<CartDto> AddToCartAsync(string? userId, string? sessionId, int productId, int quantity);
    Task<CartDto> UpdateCartItemAsync(string? userId, string? sessionId, int cartItemId, int quantity);
    Task<CartDto> RemoveFromCartAsync(string? userId, string? sessionId, int cartItemId);
    Task<CartDto> ClearCartAsync(string? userId, string? sessionId);
    Task MergeCartsAsync(string userId, string sessionId);
    Task<int> GetCartItemCountAsync(string? userId, string? sessionId);
    Task<CartDto> AddCustomizedItemToCartAsync(string? userId, string? sessionId, AddCustomizedItemToCartDto dto);
}

public interface IOrderService
{
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto?> GetOrderByNumberAsync(string orderNumber);
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
    Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(OrderFilterDto filter);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderDto> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<string> GenerateOrderNumberAsync();
}

public interface IWishlistService
{
    Task<WishlistDto> GetWishlistAsync(string userId);
    Task<bool> AddToWishlistAsync(string userId, int productId);
    Task<bool> RemoveFromWishlistAsync(string userId, int productId);
    Task<bool> IsInWishlistAsync(string userId, int productId);
    Task<int> GetWishlistCountAsync(string userId);
}

public interface IReviewService
{
    Task<PagedResultDto<ReviewDto>> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<ReviewDto> CreateReviewAsync(string userId, CreateReviewDto dto);
    Task ApproveReviewAsync(int reviewId);
    Task DeleteReviewAsync(int reviewId);
    Task<IEnumerable<ReviewDto>> GetPendingReviewsAsync();
}

public interface ICouponService
{
    Task<CouponValidationResultDto> ValidateCouponAsync(string code, decimal orderTotal);
    Task<IEnumerable<CouponDto>> GetAllCouponsAsync();
    Task<CouponDto> CreateCouponAsync(CreateCouponDto dto);
    Task<CouponDto> UpdateCouponAsync(int id, UpdateCouponDto dto);
    Task DeleteCouponAsync(int id);
    Task IncrementUsageAsync(string code);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string folder);
    Task DeleteAsync(string filePath);
    Task<bool> ExistsAsync(string filePath);
}

public interface IEmailService
{
    Task SendOrderConfirmationAsync(string email, OrderDto order);
    Task SendOrderStatusUpdateAsync(string email, OrderDto order);
    Task SendWelcomeEmailAsync(string email, string name);
    Task SendPasswordResetAsync(string email, string resetLink);
}

public interface IWhiteCoatVariantService
{
    Task<WhiteCoatVariantDetailDto?> GetVariantByIdAsync(int id);
    Task<WhiteCoatCustomerOptionsDto> GetCustomerOptionsAsync(int productId);
    Task<PagedResultDto<WhiteCoatVariantAdminDto>> GetVariantsAsync(WhiteCoatVariantFilterDto filter);
    Task<IEnumerable<WhiteCoatVariantListDto>> GetLowStockVariantsAsync();
    Task<WhiteCoatVariantDetailDto> CreateVariantAsync(CreateWhiteCoatVariantDto dto);
    Task<WhiteCoatVariantDetailDto> UpdateVariantAsync(int id, UpdateWhiteCoatVariantDto dto);
    Task DeleteVariantAsync(int id);
    Task BulkUpdateAsync(BulkUpdateWhiteCoatVariantDto dto);
    Task<WhiteCoatVariantImageDto> AddImageAsync(int variantId, string url, string? altText, bool isPrimary);
    Task RemoveImageAsync(int imageId);
}

public interface IEmbroideryService
{
    Task<IEnumerable<EmbroideryFontDto>> GetActiveFontsAsync();
    Task<IEnumerable<EmbroideryFontDto>> GetAllFontsAsync();
    Task<EmbroideryFontDto> CreateFontAsync(CreateEmbroideryFontDto dto);
    Task UpdateFontAsync(int id, CreateEmbroideryFontDto dto);
    Task DeleteFontAsync(int id);
}

public interface ILogoService
{
    Task<IEnumerable<LogoDto>> GetActiveLogosAsync(string? type = null);
    Task<IEnumerable<LogoDto>> GetAllLogosAsync();
    Task<LogoDto> CreateLogoAsync(CreateLogoDto dto);
    Task UpdateLogoAsync(int id, CreateLogoDto dto);
    Task DeleteLogoAsync(int id);
}
public interface IPriceCalculationService
{
    Task<decimal> CalculateCustomizationFeeAsync(CreateCustomizationDto customization);
    Task<(decimal EmbroideryCost, decimal LogoCost)> GetCustomizationCostsAsync(CreateCustomizationDto customization);
    Task<decimal> CalculateVariantFinalPriceAsync(int whiteCoatVariantId);
}

public interface ICustomizationPreviewService
{
    Task<WhiteCoatPreviewResultDto> GeneratePreviewAsync(WhiteCoatPreviewRequestDto request);
}

public interface ICustomizationService
{
    Task<CustomizationDto?> GetByIdAsync(int id);
    Task<int> CreateCustomizationAsync(CreateCustomizationDto dto);
    Task DeleteCustomizationAsync(int id);
}

public interface IWhiteCoatInventoryService
{
    Task<bool> IsInStockAsync(int variantId, int requestedQuantity);
    Task ReserveStockAsync(int variantId, int quantity);
    Task ReleaseStockAsync(int variantId, int quantity);
}

public interface IShippingCustomizationService
{
    Task<ShippingCustomizationDto?> GetByCustomizationIdAsync(int customizationId);
    Task<ShippingCustomizationDto> SetAsync(CreateShippingCustomizationDto dto);
    Task<decimal> CalculateExtraFeeAsync(int customizationId);
    Task<IEnumerable<CustomizationHistoryDto>> GetHistoryAsync(int customizationId);
}

/// <summary>Calculates checkout shipping cost and delivery estimate for Egyptian governorates.</summary>
public interface IShippingCalculationService
{
    IReadOnlyList<string> GetGovernorates();
    ShippingCalculationResultDto Calculate(string governorate, decimal subTotal, string? city = null);
}
