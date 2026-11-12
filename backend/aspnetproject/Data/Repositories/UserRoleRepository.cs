using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class UserRoleRepository : BaseRepository<UserRole>
{
    public UserRoleRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> UserRoleExists(int userId, int roleId)
    {
        return await ExistsAsync(userRole => userRole.UserId == userId && userRole.RoleId == roleId);
    }

    public async Task<bool> DeleteUserRole(int userId, int roleId)
    {
        int deletedCount = await _dbSet.Where(userRole => userRole.UserId == userId && userRole.RoleId == roleId)
            .ExecuteDeleteAsync();

        return deletedCount > 0;
    }
}