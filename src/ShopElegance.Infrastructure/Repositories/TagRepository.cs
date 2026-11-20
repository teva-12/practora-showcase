using Microsoft.EntityFrameworkCore;
using ShopElegance.Core.Entities;
using ShopElegance.Core.Interfaces.Repositories;
using ShopElegance.Infrastructure.Data;

namespace ShopElegance.Infrastructure.Repositories;

public class TagRepository : GenericRepository<Tag>, ITagRepository
{
    public TagRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Tag?> GetByNameAsync(string name)
        => await _dbSet.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

    public async Task<Tag?> GetBySlugAsync(string slug)
        => await _dbSet.FirstOrDefaultAsync(t => t.Slug == slug);
}
