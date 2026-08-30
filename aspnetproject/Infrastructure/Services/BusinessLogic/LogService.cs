using aspnetproject.Data;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class LogService
{
    private readonly ApplicationDbContext _dbContext;

    public LogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    
}