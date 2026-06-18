using System.Security.Claims;

namespace AppointmentAPP.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id is null || !Guid.TryParse(id, out var guid))
                throw new UnauthorizedAccessException("Invalid token.");
            return guid;
        }
    }
}
