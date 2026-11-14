using System.Net.Http.Json;
using aspnetproject.Common.Domain;

namespace aspnetproject.IntegrationTests.Helpers;

// Reads the parts of the response envelope that tests assert on.
public static class ResponseReader
{
    public static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<T>>();
        return body!.Data!;
    }

    public static async Task<List<T>> ReadListAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ListResponse<T>>();
        return body!.Data;
    }

    public static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        return body!.Message;
    }
}
