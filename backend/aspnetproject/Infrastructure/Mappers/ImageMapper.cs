using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Images;

namespace aspnetproject.Infrastructure.Mappers;

// Shared by the post and message mappers, because both own their photos the same way.
public static class ImageMapper
{
    // The addresses come in upload order, which becomes the display order.
    public static List<TImage> ToEntities<TImage>(IReadOnlyList<string> imageAddresses) where TImage : BaseImage, new()
    {
        return imageAddresses
            .Select((address, index) => new TImage { Address = address, OrderIndex = index })
            .ToList();
    }

    public static List<ImageDto> ToImageDtos(IEnumerable<BaseImage> images)
    {
        return images
            .OrderBy(image => image.OrderIndex)
            .Select(image => new ImageDto
            {
                Id = image.Id,
                Address = image.Address,
                OrderIndex = image.OrderIndex
            })
            .ToList();
    }
}
