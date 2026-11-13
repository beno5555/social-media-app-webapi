using System.Net.Http.Headers;

namespace aspnetproject.IntegrationTests.Helpers;

// Builds the multipart/form-data body of POST api/posts.
public static class PostForm
{
    public static MultipartFormDataContent Create(string? title, string? content, params PostFormFile[] files)
    {
        var form = new MultipartFormDataContent();

        if (title is not null)
        {
            form.Add(new StringContent(title), "PostTitle");
        }

        if (content is not null)
        {
            form.Add(new StringContent(content), "PostContent");
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
