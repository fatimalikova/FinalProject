using AppointmentAPP.Models;
using System.Security.Claims;

namespace AppointmentAPP.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(AppUser user, IList<string> roles, IConfiguration config);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, IConfiguration config);
    }
}
