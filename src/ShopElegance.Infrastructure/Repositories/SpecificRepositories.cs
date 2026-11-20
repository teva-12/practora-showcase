using Microsoft.EntityFrameworkCore;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Enums;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Infrastructure.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Category?> GetBySlugAsync(string slug)
        => await _dbSet.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.Slug == slug);

    public async Task<IEnumerable<Category>> GetActiveWithProductCountAsync()
        => await _dbSet.Where(c => c.IsActive)
                       .Include(c => c.Products)
                       .Include(c => c.SubCategories)
                       .OrderBy(c => c.SortOrder)
                       .ToListAsync();

    public async Task<IEnumerable<Category>> GetTopLevelAsync()
        => await _dbSet.Where(c => c.ParentCategoryId == null && c.IsActive)
                       .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                       .OrderBy(c => c.SortOrder)
                       .ToListAsync();

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Slug == slug);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }
}

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Order?> GetWithDetailsAsync(int id)
        => await _dbSet.Include(o => o.Items).ThenInclude(i => i.Product)
                       .Include(o => o.Items).ThenInclude(i => i.WhiteCoatVariant)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.EmbroideryFont)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.Logo)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.ShippingCustomization)
                       .Include(o => o.User)
                       .Include(o => o.StatusHistories)
                       .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        => await _dbSet.Include(o => o.Items).ThenInclude(i => i.Product)
                       .Include(o => o.Items).ThenInclude(i => i.WhiteCoatVariant)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.EmbroideryFont)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.Logo)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.ShippingCustomization)
                       .Include(o => o.StatusHistories)
                       .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        => await _dbSet.Where(o => o.UserId == userId)
                       .Include(o => o.Items).ThenInclude(i => i.Product)
                       .Include(o => o.Items).ThenInclude(i => i.WhiteCoatVariant)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.EmbroideryFont)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.Logo)
                       .Include(o => o.Items).ThenInclude(i => i.Customization)!.ThenInclude(c => c!.ShippingCustomization)
                       .OrderByDescending(o => o.CreatedAt)
                       .ToListAsync();

    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? status = null,
        string? search = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.Include(o => o.User).Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            query = query.Where(o => o.Status == orderStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(term)
                                  || o.ShippingEmail.ToLower().Contains(term)
                                  || o.ShippingFirstName.ToLower().Contains(term)
                                  || o.ShippingLastName.ToLower().Contains(term));
        }

        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);

        var total = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();
        return (orders, total);
    }

    public async Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year)
    {
        var data = await _dbSet
            .Where(o => o.CreatedAt.Year == year && o.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(o => o.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Revenue = g.Sum(o => o.Total) })
            .ToListAsync();

        var result = new Dictionary<string, decimal>();
        for (int m = 1; m <= 12; m++)
        {
            var monthName = new DateTime(year, m, 1).ToString("MMM");
            result[monthName] = data.FirstOrDefault(d => d.Month == m)?.Revenue ?? 0;
        }
        return result;
    }

    public async Task<Dictionary<string, int>> GetOrdersByStatusAsync()
    {
        var data = await _dbSet.GroupBy(o => o.Status)
                               .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                               .ToListAsync();
        return data.ToDictionary(d => d.Status, d => d.Count);
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.Where(o => o.PaymentStatus == PaymentStatus.Paid);
        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
        return await query.SumAsync(o => o.Total);
    }

    public async Task<int> GetTotalOrdersCountAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _dbSet.AsQueryable();
        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
        return await query.CountAsync();
    }
}

public class CartRepository : GenericRepository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Cart?> GetByUserIdAsync(string userId)
        => await _dbSet.FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Cart?> GetBySessionIdAsync(string sessionId)
        => await _dbSet.FirstOrDefaultAsync(c => c.SessionId == sessionId);

    public async Task<Cart?> GetWithItemsAsync(int cartId)
        => await _dbSet.Include(c => c.Items).ThenInclude(i => i.Product)
                       .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
                       .Include(c => c.Items).ThenInclude(i => i.WhiteCoatVariant)
                       .Include(c => c.Items).ThenInclude(i => i.Customization)!.ThenInclude(cu => cu!.EmbroideryFont)
                       .Include(c => c.Items).ThenInclude(i => i.Customization)!.ThenInclude(cu => cu!.Logo)
                       .FirstOrDefaultAsync(c => c.Id == cartId);

    public async Task<Cart?> GetWithItemsByUserIdAsync(string userId)
        => await _dbSet.Include(c => c.Items).ThenInclude(i => i.Product)
                       .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
                       .Include(c => c.Items).ThenInclude(i => i.WhiteCoatVariant)
                       .Include(c => c.Items).ThenInclude(i => i.Customization)!.ThenInclude(cu => cu!.EmbroideryFont)
                       .Include(c => c.Items).ThenInclude(i => i.Customization)!.ThenInclude(cu => cu!.Logo)
                       .FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Cart?> GetWithItemsBySessionIdAsync(string sessionId)
        => await _dbSet.Include(c => c.Items).ThenInclude(i => i.Product)
                       .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
                       .Include(c => c.Items).ThenInclude(i => i.WhiteCoatVariant)
                       .Include(c => c.Items).ThenInclude(i => i.Customization)!.ThenInclude(cu => cu!.EmbroideryFont)
                       .Include(c => c.Items).ThenInclude(i => i.Customization)!.ThenInclude(cu => cu!.Logo)
                       .FirstOrDefaultAsync(c => c.SessionId == sessionId);
}

public class WishlistRepository : GenericRepository<Wishlist>, IWishlistRepository
{
    public WishlistRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Wishlist?> GetByUserIdAsync(string userId)
        => await _dbSet.FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<Wishlist?> GetWithItemsAsync(string userId)
        => await _dbSet.Include(w => w.Items).ThenInclude(i => i.Product)
                       .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
                       .FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<bool> HasProductAsync(string userId, int productId)
        => await _context.WishlistItems
                         .AnyAsync(wi => wi.Wishlist.UserId == userId && wi.ProductId == productId);
}

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetProductReviewsAsync(
        int productId, int page, int pageSize, bool approvedOnly = true)
    {
        var query = _dbSet.Where(r => r.ProductId == productId)
                          .Include(r => r.User)
                          .AsQueryable();
        if (approvedOnly) query = query.Where(r => r.IsApproved);
        var total = await query.CountAsync();
        var reviews = await query.OrderByDescending(r => r.CreatedAt)
                                 .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (reviews, total);
    }

    public async Task<bool> UserReviewedProductAsync(string userId, int productId)
        => await _dbSet.AnyAsync(r => r.UserId == userId && r.ProductId == productId);

    public async Task<bool> IsVerifiedPurchaseAsync(string userId, int productId)
        => await _context.OrderItems.AnyAsync(oi => oi.ProductId == productId && oi.Order.UserId == userId
                                              && oi.Order.PaymentStatus == PaymentStatus.Paid);

    public async Task<IEnumerable<Review>> GetPendingReviewsAsync()
        => await _dbSet.Where(r => !r.IsApproved)
                       .Include(r => r.User)
                       .Include(r => r.Product)
                       .OrderByDescending(r => r.CreatedAt)
                       .ToListAsync();
}

public class CouponRepository : GenericRepository<Coupon>, ICouponRepository
{
    public CouponRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Coupon?> GetByCodeAsync(string code)
        => await _dbSet.FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Code.ToUpper() == code.ToUpper());
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }
}

public class AddressRepository : GenericRepository<Address>, IAddressRepository
{
    public AddressRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Address>> GetUserAddressesAsync(string userId)
        => await _dbSet.Where(a => a.UserId == userId).OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<Address?> GetDefaultAsync(string userId)
        => await _dbSet.FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

    public async Task SetDefaultAsync(string userId, int addressId)
    {
        await _dbSet.Where(a => a.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
        await _dbSet.Where(a => a.Id == addressId).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, true));
    }
}
