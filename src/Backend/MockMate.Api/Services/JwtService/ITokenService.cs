using System.Security.Claims;
using MockMate.Api.Entities;

namespace MockMate.Api.Services.JwtService;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles, IList<Claim> userClaims);
    RefreshToken GenerateRefreshToken(string ipAddress);
}
