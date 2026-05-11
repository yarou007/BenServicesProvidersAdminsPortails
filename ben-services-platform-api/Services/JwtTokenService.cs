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
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, admin.Email),
            new(ClaimTypes.Email, admin.Email),
            new(ClaimTypes.Name, admin.Username),
            new("username", admin.Username),
            new(ClaimTypes.Role, admin.Role),
            new("role", admin.Role)
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
