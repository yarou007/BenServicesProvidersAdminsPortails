using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Settings;
using Microsoft.IdentityModel.Tokens;

namespace BenServicesPlatform.Api.Services;

public class JwtTokenService(JwtSettings jwtSettings) : IJwtTokenService
{
    public string GenerateToken(AdminEntity admin)
    {
        return GenerateToken(
            userId: admin.Id,
            email: admin.Email,
            username: admin.Username,
            role: admin.Role);
    }

    public string GenerateToken(ProviderAccountEntity providerAccount)
    {
        return GenerateToken(
            userId: providerAccount.Id,
            email: providerAccount.Email,
            username: providerAccount.Email,
            role: providerAccount.Role);
    }

    private string GenerateToken(int userId, string email, string username, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, username),
            new("username", username),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
