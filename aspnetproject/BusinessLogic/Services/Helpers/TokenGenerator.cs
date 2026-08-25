using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace aspnetproject.BusinessLogic.Services.Helpers;

public class TokenGenerator
{
    private readonly IConfiguration _configuration;

    public TokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GenerateAccessToken(int userId, string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name,           username),
        };

        var jwtSection = _configuration.GetSection("Jwt");
        var minutes    = int.Parse(jwtSection["AccessTokenMinutes"]!);

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(minutes),
            SigningCredentials = credentials
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var token = Convert.ToBase64String(bytes);
        
        return token;
    }

    public string HashToken(string token)
    {
        var    tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        string tokenHash  = Convert.ToBase64String(tokenBytes);
        
        return tokenHash;
    }
}