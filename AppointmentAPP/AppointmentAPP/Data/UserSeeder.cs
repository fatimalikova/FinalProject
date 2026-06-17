using AppointmentAPP.Models;
using Microsoft.AspNetCore.Identity;

namespace AppointmentAPP.Data
{
    public class UserSeeder
    {
        public static async Task SeedAsync(UserManager<User> userManager)
        {
            var adminEmail = "admin@admin.com";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Administrator"
                };

                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
