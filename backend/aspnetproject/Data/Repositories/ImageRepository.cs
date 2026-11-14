using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

// Reads across both photo tables, so it is not tied to one entity type the way BaseRepository<T> is.
public class ImageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ImageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // The ones among addresses that a post's photo or a message's photo still holds.
    public async Task<List<string>> GetReferencedAddressesAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken)
    {
        var postImageAddresses = _dbContext.PostImages
            .Where(image => addresses.Contains(image.Address))
            .Select(image => image.Address);

        var messageImageAddresses = _dbContext.MessageImages
            .Where(image => addresses.Contains(image.Address))
            .Select(image => image.Address);

        return await postImageAddresses
            .Union(messageImageAddresses)
            .ToListAsync(cancellationToken);
    }
}
