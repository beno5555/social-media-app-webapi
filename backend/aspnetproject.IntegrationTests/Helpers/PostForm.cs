namespace aspnetproject.IntegrationTests.Helpers;

// Builds the multipart/form-data body of POST api/posts.
public static class PostForm
{
    public static MultipartFormDataContent Create(string? title, string? content, params ImageFormFile[] files)
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

        ImageFormFiles.AddTo(form, files);

        return form;
    }
}
