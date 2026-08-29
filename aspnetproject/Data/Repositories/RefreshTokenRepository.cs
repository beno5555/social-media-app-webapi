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
        await _dbSet.Where(refreshToken => refreshToken.UserId == userId)
            .ExecuteUpdateAsync(setters =>
            {
                setters.SetProperty(refreshToken => refreshToken.RevokedAt, DateTime.UtcNow);
            });
    }
}