using System.Net.Http.Headers;

namespace aspnetproject.IntegrationTests.Helpers;

// Builds the multipart/form-data body of POST api/messages/{receiverId}.
public static class MessageForm
{
    public static MultipartFormDataContent Create(string? content, params PostFormFile[] files)
    {
        var form = new MultipartFormDataContent();

        if (content is not null)
        {
            form.Add(new StringContent(content), "Content");
        }

        foreach (var file in files)
        {
            var fileContent = new ByteArrayContent(file.Bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "Images", file.FileName);
        }

        return form;
    }
}
