using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class RefreshTokenRepository : BaseEntityRepository<RefreshToken>
{
    public RefreshTokenRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        
    }

    public async Task<RefreshToken?> GetActiveByHashAsync(string tokenHash)
    {
        var token = await _dbSet.FirstOrDefaultAsync(refreshToken => 
            refreshToken.TokenHash == tokenHash &&
            refreshToken.RevokedAt == null      &&
            refreshToken.ExpiresAt > DateTime.UtcNow);

        return token;
    }


    public async Task RevokeAsync(RefreshToken refreshToken)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(int userId)
    {
        await _dbSet.Where(refreshToken => refreshToken.UserId == userId && refreshToken.RevokedAt == null)
            .ExecuteUpdateAsync(setters =>
            {
                setters.SetProperty(refreshToken => refreshToken.RevokedAt, DateTime.UtcNow);
            });
    }

    // background job methods
    public async Task<int> DeleteExpiredAsync(int batchSize, CancellationToken cancellationToken)
    {
        int deletedCount = 0;
        var ids = await _dbSet
            .Where(refreshToken => refreshToken.ExpiresAt < DateTime.UtcNow && refreshToken.RevokedAt != null)
            .OrderBy(refreshToken => refreshToken.Id)
            .Take(batchSize)
            .Select(refreshToken => refreshToken.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count > 0)
        {
            deletedCount = await _dbSet
                .Where(refreshToken => ids.Contains(refreshToken.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return deletedCount;
    }
}