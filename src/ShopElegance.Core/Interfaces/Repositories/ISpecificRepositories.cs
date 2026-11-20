using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;

namespace ShopElegance.Core.Interfaces.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug);
    Task<Product?> GetWithDetailsAsync(int id);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? categoryId = null,
        string? search = null, string? sortBy = null,
        decimal? minPrice = null, decimal? maxPrice = null,
        bool? inStock = null, string? productType = null);
    Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8);
    Task<IEnumerable<Product>> GetRelatedAsync(int productId, int categoryId, int count = 4);
    Task<IEnumerable<Product>> GetLowStockAsync(int threshold = 5);
    Task<IEnumerable<Product>> GetBestSellersAsync(int count = 8);
    Task IncrementViewCountAsync(int productId);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
}

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug);
    Task<IEnumerable<Category>> GetActiveWithProductCountAsync();
    Task<IEnumerable<Category>> GetTopLevelAsync();
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
}

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithDetailsAsync(int id);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
    Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? status = null,
        string? search = null, DateTime? from = null, DateTime? to = null);
    Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year);
    Task<Dictionary<string, int>> GetOrdersByStatusAsync();
    Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null);
    Task<int> GetTotalOrdersCountAsync(DateTime? from = null, DateTime? to = null);
}

public interface ICartRepository : IGenericRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(string userId);
    Task<Cart?> GetBySessionIdAsync(string sessionId);
    Task<Cart?> GetWithItemsAsync(int cartId);
    Task<Cart?> GetWithItemsByUserIdAsync(string userId);
    Task<Cart?> GetWithItemsBySessionIdAsync(string sessionId);
}

public interface IWishlistRepository : IGenericRepository<Wishlist>
{
    Task<Wishlist?> GetByUserIdAsync(string userId);
    Task<Wishlist?> GetWithItemsAsync(string userId);
    Task<bool> HasProductAsync(string userId, int productId);
}

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<(IEnumerable<Review> Reviews, int TotalCount)> GetProductReviewsAsync(
        int productId, int page, int pageSize, bool approvedOnly = true);
    Task<bool> UserReviewedProductAsync(string userId, int productId);
    Task<bool> IsVerifiedPurchaseAsync(string userId, int productId);
    Task<IEnumerable<Review>> GetPendingReviewsAsync();
}

public interface ICouponRepository : IGenericRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
}

public interface IAddressRepository : IGenericRepository<Address>
{
    Task<IEnumerable<Address>> GetUserAddressesAsync(string userId);
    Task<Address?> GetDefaultAsync(string userId);
    Task SetDefaultAsync(string userId, int addressId);
}

public interface IWhiteCoatVariantRepository : IGenericRepository<WhiteCoatVariant>
{
    Task<WhiteCoatVariant?> GetWithDetailsAsync(int id);
    Task<IEnumerable<WhiteCoatVariant>> GetByProductIdAsync(int productId, bool activeOnly = true);
    Task<(IEnumerable<WhiteCoatVariant> Variants, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? productId = null, string? search = null,
        string? size = null, string? color = null, bool? isActive = null,
        bool? lowStockOnly = null, string? sortBy = null);
    Task<IEnumerable<WhiteCoatVariant>> GetLowStockAsync();
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    Task AdjustStockAsync(int variantId, int delta);
    Task<WhiteCoatVariant?> GetByImageIdAsync(int imageId);
}

public interface IEmbroideryFontRepository : IGenericRepository<EmbroideryFont>
{
    Task<IEnumerable<EmbroideryFont>> GetActiveAsync();
}

public interface ILogoRepository : IGenericRepository<Logo>
{
    Task<IEnumerable<Logo>> GetActiveAsync(LogoSourceType? type = null);
}

public interface ICustomizationRepository : IGenericRepository<Customization>
{
    Task<Customization?> GetWithDetailsAsync(int id);
}

public interface ICustomizationHistoryRepository : IGenericRepository<CustomizationHistory>
{
    Task<IEnumerable<CustomizationHistory>> GetForCustomizationAsync(int customizationId);
    Task LogChangeAsync(int customizationId, string changedField, string? oldValue, string? newValue, string? changedByUserId);
}

public interface IShippingCustomizationRepository : IGenericRepository<ShippingCustomization>
{
    Task<ShippingCustomization?> GetByCustomizationIdAsync(int customizationId);
}
