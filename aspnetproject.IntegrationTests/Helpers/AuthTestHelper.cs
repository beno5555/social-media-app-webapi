using System.Net.Http.Json;

namespace aspnetproject.IntegrationTests.Helpers;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
}

public static class AuthTestHelper
{
    public static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("api/auth/login", new
        {
            UniqueIdentifier = username,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    public static void AttachToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
}