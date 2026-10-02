using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LearningOS.Models;
using Microsoft.IdentityModel.Tokens;

namespace LearningOS.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    public const string DefaultSecretKey = "LearningOS_Authoritative_SuperSecret_Jwt_SigningKey_2026_Mastery_FullStack!";

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(AppUser user)
    {
        var secret = _config["Jwt:SecretKey"] 
                  ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                  ?? DefaultSecretKey;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("status", user.Status)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(30),
            SigningCredentials = creds,
            Issuer = "LearningOS",
            Audience = "LearningOS_Client"
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
