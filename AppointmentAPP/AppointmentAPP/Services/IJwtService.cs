using AppointmentAPP.Models;
using System.Security.Claims;

namespace AppointmentAPP.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user, IList<string> roles, IConfiguration config);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, IConfiguration config);
    }
}
