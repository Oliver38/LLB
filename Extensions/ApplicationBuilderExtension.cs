using LLB.Models;
using LLB.Data;
using Microsoft.AspNetCore.Identity;
//using LLB.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace LLB.Extensions
{
    public static class ApplicationBuilderExtension
    {

        public static async Task<IApplicationBuilder> InitialiseRoles(this IApplicationBuilder app)
        {
            var scope = app.ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope();
            if (scope != null)
            {
                var roles = new[]
                {
                    "external", "super user", "shuttle", "admin", "client",
                };

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                foreach (var role in roles)
                {
                    // var roleStore = new RoleStore<IdentityRole>(context);
                    //
                    // if (!context.Roles.Any(r => r.Name == role))
                    // {
                    //     await roleStore.CreateAsync(new IdentityRole{Name = role});
                    // }

                    if (await roleManager.RoleExistsAsync(role)) continue;
                    var identityRole = new IdentityRole { Name = role };
                    await roleManager.CreateAsync(identityRole);
                }
            }

            return app;
        }

     /*   public static async Task<IApplicationBuilder> InitialiseUsers(this IApplicationBuilder app)
        {
            var scope = app.ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope();
            if (scope == null) return app;
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var admin = new ApplicationUser
            {
                Email = "admin@admin.com",
                Name = "Admin",
                LastName = "Admin",
                //IdPassport = "admin",
                //PassportUpload = "admin",
                //Country = "admin",
                PhysicalAddress = "admin",
                PhoneNumber = "admin",
              //  EmployeeNumber = "admin",
                //Office = "Harare",
                UserName = "admin"
            };

            try
            {
                var result = await userManager.CreateAsync(admin, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("admin"))
                        await userManager.AddToRoleAsync(admin, "admin");

                    if (await roleManager.RoleExistsAsync("internal"))
                        await userManager.AddToRoleAsync(admin, "internal");
                }
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                // throw;
            }



            return app;
        }*/

        public static void InitialiseDatabase(this IApplicationBuilder app)
        {
            var scope = app.ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope();
            if (scope != null)
            {
                using (var context = scope.ServiceProvider.GetRequiredService<AppDbContext>())
                {
                    try
                    {
                        context.Database.Migrate();
                        // Do data seeding here
                    }
                    catch (Exception ex)
                    {
                        // Log exception here
                       // throw ex;
                    }
                }
            }

            // return app;
        }
    }
}
