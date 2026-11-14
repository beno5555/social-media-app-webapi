using System.Net;

namespace aspnetproject.IntegrationTests.Helpers;

public static class RateLimitExhaustHelper
{
    // Comfortably above the largest per-policy permit limit under test, and far below the global limiter's,
    // so only the policy under test can reject.
    private const int MaxAttempts = 100;

    // Sends the request until the status is no longer stillAllowedStatus (or MaxAttempts is reached) and returns the last response.
    public static async Task<HttpResponseMessage> ExhaustAsync(Func<Task<HttpResponseMessage>> sendRequest, HttpStatusCode stillAllowedStatus)
    {
        HttpResponseMessage response;
        var attempts = 0;
        bool stillAllowed;

        do
        {
            response = await sendRequest();
            attempts++;

            stillAllowed = response.StatusCode == stillAllowedStatus;
        }
        while (stillAllowed && attempts < MaxAttempts);

        return response;
    }
}
