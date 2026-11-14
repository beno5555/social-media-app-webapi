using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace aspnetproject.IntegrationTests.Helpers;

// Builds an access token the host would accept apart from its lifetime: signed with the host's own key and
// carrying a real user id, so a test can tell "rejected because expired" from "rejected because forged".
// It mirrors TokenGenerator.GenerateAccessToken (key, algorithm, user id claim), which cannot be given a past expiry;
// keep the two in step.
public static class ExpiredTokenFactory
{
    private const string JwtKeySetting = "Jwt:Key";

    // Fixed times in the past, so the token is always expired whatever the clock skew allowance.
    private static readonly DateTime IssuedAt  = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiresAt = new(2026, 1, 1, 10, 15, 0, DateTimeKind.Utc);

    public static string Create(CustomWebApplicationFactory factory, int userId)
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var key           = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[JwtKeySetting]!));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())]),
            IssuedAt           = IssuedAt,
            NotBefore          = IssuedAt,
            Expires            = ExpiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token        = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        return token;
    }
}
