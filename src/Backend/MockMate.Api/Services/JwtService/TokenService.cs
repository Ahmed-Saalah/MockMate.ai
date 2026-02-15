using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockMate.Api.Configuration;
using MockMate.Api.Entities;

namespace MockMate.Api.Services.JwtService;

public class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateAccessToken(User user, IList<string> roles, IList<Claim> userClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(
            userClaims.Where(c =>
                c.Type != ClaimTypes.NameIdentifier
                && c.Type != ClaimTypes.Email
                && c.Type != ClaimTypes.Name
            )
        );

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        return new RefreshToken
        {
            Token = GetRandomTokenString(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
        };
    }

    private static string GetRandomTokenString()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
