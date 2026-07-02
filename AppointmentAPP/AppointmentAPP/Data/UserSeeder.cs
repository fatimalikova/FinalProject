using AppointmentAPP.Models;
using Microsoft.AspNetCore.Identity;

namespace AppointmentAPP.Data
{
    public class UserSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            const string adminEmail = "fmelikova49@gmail.com"; 
            const string adminPassword = "Admin@123";
            const string adminUserName = "_admin";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin is not null)
                return; // artıq seed olunub, təkrar yaratma

            var admin = new AppUser
            {
                FullName = "System Admin",
                Email = adminEmail,
                UserName = adminUserName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Admin seed failed: {errors}");
            }
        }
    }
}
