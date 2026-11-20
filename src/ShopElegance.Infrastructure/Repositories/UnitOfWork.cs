using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private IOrderRepository? _orders;
    private ICartRepository? _carts;
    private IWishlistRepository? _wishlists;
    private IReviewRepository? _reviews;
    private ICouponRepository? _coupons;
    private IAddressRepository? _addresses;

    private ITagRepository? _tags;

    private IWhiteCoatVariantRepository? _whiteCoatVariants;
    private IEmbroideryFontRepository? _embroideryFonts;
    private ILogoRepository? _logos;
    private ICustomizationRepository? _customizations;
    private ICustomizationHistoryRepository? _customizationHistory;
    private IShippingCustomizationRepository? _shippingCustomizations;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public ICartRepository Carts => _carts ??= new CartRepository(_context);
    public IWishlistRepository Wishlists => _wishlists ??= new WishlistRepository(_context);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);
    public ICouponRepository Coupons => _coupons ??= new CouponRepository(_context);
    public IAddressRepository Addresses => _addresses ??= new AddressRepository(_context);
    public ITagRepository Tags => _tags ??= new TagRepository(_context);

    public IWhiteCoatVariantRepository WhiteCoatVariants => _whiteCoatVariants ??= new WhiteCoatVariantRepository(_context);
    public IEmbroideryFontRepository EmbroideryFonts => _embroideryFonts ??= new EmbroideryFontRepository(_context);
    public ILogoRepository Logos => _logos ??= new LogoRepository(_context);
    public ICustomizationRepository Customizations => _customizations ??= new CustomizationRepository(_context);
    public ICustomizationHistoryRepository CustomizationHistory => _customizationHistory ??= new CustomizationHistoryRepository(_context);
    public IShippingCustomizationRepository ShippingCustomizations => _shippingCustomizations ??= new ShippingCustomizationRepository(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null) return;
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null) return;
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose() => _context.Dispose();
}
