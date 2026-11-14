namespace aspnetproject.IntegrationTests.Helpers;

// Builds the multipart/form-data body of POST api/messages/{receiverId}.
public static class MessageForm
{
    public static MultipartFormDataContent Create(string? content, params ImageFormFile[] files)
    {
        var form = new MultipartFormDataContent();

        if (content is not null)
        {
            form.Add(new StringContent(content), "Content");
        }

        ImageFormFiles.AddTo(form, files);

        return form;
    }
}
