using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;

namespace aspnetproject.Data.Repositories;

public class RoleRepository : BaseEntityRepository<Role>
{
    public RoleRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Role?> GetRoleByName(string roleName)
    {
        return await GetFirstAsync(role => role.Name == roleName);
    }

    public async Task<bool> ExistsByNameAsync(string roleName)
    {
        return await ExistsAsync(role => role.Name == roleName);
    }
}