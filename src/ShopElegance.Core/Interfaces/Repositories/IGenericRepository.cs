using System.Linq.Expressions;

namespace ShopElegance.Core.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IOrderRepository Orders { get; }
    ICartRepository Carts { get; }
    IWishlistRepository Wishlists { get; }
    IReviewRepository Reviews { get; }
    ICouponRepository Coupons { get; }
    IAddressRepository Addresses { get; }
    ITagRepository Tags { get; }
    IWhiteCoatVariantRepository WhiteCoatVariants { get; }
    IEmbroideryFontRepository EmbroideryFonts { get; }
    ILogoRepository Logos { get; }
    ICustomizationRepository Customizations { get; }
    ICustomizationHistoryRepository CustomizationHistory { get; }
    IShippingCustomizationRepository ShippingCustomizations { get; }
    Task<int> SaveChangesAsync();

    /// <summary>Wraps multi-step writes (e.g. checkout) so a failure anywhere rolls back everything.</summary>
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
