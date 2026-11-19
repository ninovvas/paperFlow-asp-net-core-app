
namespace PaperFlow.Web.Identity.Seeding
{
    using Microsoft.AspNetCore.Identity;
    using PaperFlow.Common.Constants;


public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (string role in new[] { RoleConstants.User, RoleConstants.Administrator })
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            IdentityResult result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not create role '{role}': {errors}");
            }
        }
    }
}
}
