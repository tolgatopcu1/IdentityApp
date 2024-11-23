using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityApp.Models
{
    public static class IdentitySeedData
    {
        private const string adminUser="admin";
        private const string adminPassword="Admin_123";
        
        public static async void IdentityTestUser(IApplicationBuilder app)
        {
            var context = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<IdentityContext>();

            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

            var userManager = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            var user = await userManager.FindByNameAsync(adminUser);

            if (user == null)
            {
                user = new AppUser{
                    FullName = "Tolga Topçu",
                    UserName = adminUser,
                    Email = "admin@tolgatopcu.com",
                    PhoneNumber = "444444444",
                    EmailConfirmed=true
                };

                await userManager.CreateAsync(user,adminPassword);
            }

            var roleManager = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            var role = await roleManager.FindByNameAsync("admin");

            if (role == null)
            {
                role = new AppRole{
                    Name = "admin"
                };

                await roleManager.CreateAsync(role);
            }
        }
    }
}