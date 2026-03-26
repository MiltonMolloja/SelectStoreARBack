using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SelectStoreAR.Application.Interfaces;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Services;

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(User user)
    {
        string secret = configuration["Auth:JwtSecret"]
            ?? throw new InvalidOperationException("Auth:JwtSecret is not configured");

        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        SymmetricSecurityKey key = new(keyBytes);
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new("role", user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        int expirationDays = int.TryParse(configuration["Auth:JwtExpirationDays"], out int days) ? days : 7;

        JwtSecurityToken token = new(
            issuer: configuration["Auth:JwtIssuer"] ?? "selectstorear",
            audience: configuration["Auth:JwtAudience"] ?? "selectstorear",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
