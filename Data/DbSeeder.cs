using Microsoft.AspNetCore.Identity;
using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Seed Roles
                await SeedRoles(roleManager);

                // Seed Admin
                await SeedAdmin(userManager, context);
            }
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(ApplicationRoles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Admin));
            }
            if (!await roleManager.RoleExistsAsync(ApplicationRoles.User))
            {
                await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.User));
            }
        }

        private static async Task SeedAdmin(UserManager<IdentityUser> userManager, AppDbContext context)
        {
            // Check if admin exists
            var adminEmail = "admin@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "12345Abc!");

                if (result.Succeeded)
                {
                    // Add admin to Admin role
                    await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);

                    // Create ApplicationUser record
                    var appUser = new ApplicationUser
                    {
                        Id = admin.Id,
                        Name = "Administrator",
                        Address = "System",
                        IdentityUser = admin
                    };

                    context.ApplicationUsers.Add(appUser);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
} 