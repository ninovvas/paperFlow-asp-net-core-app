

namespace PaperFlow.Web.Identity.Seeding
{

    using Microsoft.AspNetCore.Identity;

    public static class IdentitySeedExtensions
    {
        public static async Task SeedIdentityAsync(this WebApplication app)
        {
            await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

            RoleManager<IdentityRole> roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<IdentityUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            ILogger logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("PaperFlow.Identity.Seeding");

            await RoleSeeder.SeedAsync(roleManager);
            await AdministratorSeeder.SeedAsync(userManager, app.Configuration, logger);
        }
    }
}

