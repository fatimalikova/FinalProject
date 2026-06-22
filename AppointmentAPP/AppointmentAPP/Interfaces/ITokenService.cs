using AppointmentAPP.Models;

namespace AppointmentAPP.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) GenerateToken(AppUser user, IList<string> roles);
    }
}
