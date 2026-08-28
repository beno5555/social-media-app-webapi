using aspnetproject.Data.Models;
using aspnetproject.Models;

namespace aspnetproject.Data.Repositories.Base;

public class BaseEntityRepository<T> : BaseRepository<T> where T : BaseEntity
{
    public BaseEntityRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
    
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }
    
    public async Task DeleteWithoutChangeTrackingAsync(int id)
    {
        await DeleteWhereAsync(entity => entity.Id == id);
    }
    
    public async Task<bool> ExistsByIdAsync(int id)
    {
        return await ExistsAsync(entity => entity.Id == id);
    }

    public void ClearTracker()
    {
        _dbContext.ChangeTracker.Clear();
    }

}
