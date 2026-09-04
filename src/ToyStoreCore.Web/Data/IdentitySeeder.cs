using Microsoft.AspNetCore.Identity;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Data;

public static class IdentitySeeder
{
    public const string AdminRole = "Admin";
    public const string CustomerRole = "Customer";
    public const string StaffRole = "Staff";

    // Dev-only default credentials. Change the password after first login,
    // or override via configuration before deploying anywhere real.
    private const string DefaultAdminEmail = "admin@toystorecore.local";
    private const string DefaultAdminPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { AdminRole, CustomerRole, StaffRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                FullName = "Store Admin",
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AdminRole);
            }
        }
    }
}
