using Microsoft.AspNetCore.Identity;

namespace AppointmentAPP.Data
{
    public class RoleSeeder
    {
        private static readonly string[] Roles = { "Admin", "Provider", "Client" };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            foreach (var roleName in Roles)
            {
                var exists = await roleManager.RoleExistsAsync(roleName);
                if (!exists)
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }
        }

    }
    
}
