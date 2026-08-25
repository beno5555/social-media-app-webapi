using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories.Base;

public class BaseRepository<T> where T : class
{
    protected readonly ApplicationDbContext _dbContext;
    protected readonly DbSet<T>             _dbSet;

    protected BaseRepository(ApplicationDbContext dbContext) 
    {
        _dbContext = dbContext;
        _dbSet = _dbContext.Set<T>();
    }

    /// <summary>
    /// opt for querying in batches if the db set is too large
    /// </summary>
    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    protected async Task<List<T>> GetWhereAsync(
        Expression<Func<T, bool>> predicate,
        int? pageNumber = null,
        int? pageSize = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool track = true)
    {
        var query = Query(track).Where(predicate);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }
        
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            query = query
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<List<T>> GetPaginatedAsync(int? pageNumber, int? pageSize)
    {
        List<T> list;
        if (pageNumber is not null && pageSize is not null)
        {
            list = await _dbSet
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToListAsync();
        }
        else
        {
            list = await _dbSet.ToListAsync();
        }

        return list;
    }

    protected async Task DeleteWhereAsync(Expression<Func<T, bool>> predicate)
    {
        await Query().Where(predicate).ExecuteDeleteAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    protected async Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate)
    {
        return await Query().FirstOrDefaultAsync(predicate);
    }

    protected async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    protected virtual IQueryable<T> Query(bool track = true)
    {
        var query = _dbSet;
        return track ? query : query.AsNoTracking();
    }
    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        await operation();
        await transaction.CommitAsync();
    }   
}
