namespace aspnetproject.IntegrationTests.Helpers;

// One file of a multipart upload, as posts and messages both send them.
public record ImageFormFile(byte[] Bytes, string FileName, string ContentType);
