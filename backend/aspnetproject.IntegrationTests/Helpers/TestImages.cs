using System.Text;

namespace aspnetproject.IntegrationTests.Helpers;

// Files that pass or fail the server's content check. Only the leading bytes decide the format, so the bodies are tiny.
// The marker byte makes each file's bytes unique, so a test can tell which file came back.
public static class TestImages
{
    public const string JpegContentType = "image/jpeg";
    public const string PngContentType  = "image/png";
    public const string WebPContentType = "image/webp";

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] PngSignature  = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] Jpeg(byte marker = 0) => [..JpegSignature, marker];

    public static byte[] Png(byte marker = 0) => [..PngSignature, marker];

    public static byte[] WebP(byte marker = 0) =>
        [..Encoding.ASCII.GetBytes("RIFF"), 0x10, 0x00, 0x00, 0x00, ..Encoding.ASCII.GetBytes("WEBPVP8 "), marker];

    // A script, whatever name or content type it is sent with.
    public static byte[] NotAnImage() => Encoding.UTF8.GetBytes("<?php echo 'not an image'; ?>");

    public static byte[] JpegOfSize(int sizeInBytes)
    {
        var bytes = new byte[sizeInBytes];
        JpegSignature.CopyTo(bytes, 0);
        return bytes;
    }
}
