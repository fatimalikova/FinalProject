using Microsoft.AspNetCore.Identity;

namespace AppointmentAPP.Data
{
    public class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager) {

            var roles = new[] 
            {
                new IdentityRole{
                    Id = Guid.NewGuid().ToString(),
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole{ 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Provider",
                    NormalizedName = "PROVIDER"
                },
                new IdentityRole{ 
                    Id = Guid.NewGuid().ToString(),
                    Name = "Client",
                    NormalizedName = "CLIENT"
                }
                
            };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }
    }
}
