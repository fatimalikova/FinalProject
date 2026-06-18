using AppointmentAPP.Models;

namespace AppointmentAPP.Services.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) GenerateToken(AppUser user, IList<string> roles);
    }
}
