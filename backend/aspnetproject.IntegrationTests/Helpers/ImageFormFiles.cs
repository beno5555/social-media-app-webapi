using System.Net.Http.Headers;

namespace aspnetproject.IntegrationTests.Helpers;

// Image files for the Images field, which posts and messages both bind.
public static class ImageFormFiles
{
    private const string ImagesFieldName = "Images";

    public static ImageFormFile Jpeg(byte marker) => new(TestImages.Jpeg(marker), "photo.jpg", TestImages.JpegContentType);
    public static ImageFormFile Png(byte marker)  => new(TestImages.Png(marker), "photo.png", TestImages.PngContentType);
    public static ImageFormFile WebP(byte marker) => new(TestImages.WebP(marker), "photo.webp", TestImages.WebPContentType);

    // Adds the files in order, which is the order the server stores them in.
    public static void AddTo(MultipartFormDataContent form, IEnumerable<ImageFormFile> files)
    {
        foreach (var file in files)
        {
            var fileContent = new ByteArrayContent(file.Bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, ImagesFieldName, file.FileName);
        }
    }
}
