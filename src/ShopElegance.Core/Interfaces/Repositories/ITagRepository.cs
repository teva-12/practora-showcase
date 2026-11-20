using ShopElegance.Core.Entities;

namespace ShopElegance.Core.Interfaces.Repositories;

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<Tag?> GetByNameAsync(string name);
    Task<Tag?> GetBySlugAsync(string slug);
}
