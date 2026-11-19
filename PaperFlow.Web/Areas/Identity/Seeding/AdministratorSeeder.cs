

namespace PaperFlow.Web.Identity.Seeding
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Identity;
    using PaperFlow.Common.Constants;


public static class AdministratorSeeder
{
    public static async Task SeedAsync(
        UserManager<IdentityUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        if (!configuration.GetValue<bool>("AdminSeed:Enabled"))
            return;

        string? email = configuration["AdminSeed:Email"]?.Trim();
        string? password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("AdminSeed is enabled, but a valid AdminSeed:Email and AdminSeed:Password were not provided.");
        }

        IdentityUser? existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            if (await userManager.IsInRoleAsync(existingUser, RoleConstants.Administrator))
            {
                logger.LogInformation("Administrator account already provisioned.");
                return;
            }

            throw new InvalidOperationException(
                "AdminSeed email belongs to an existing non-administrator account. " +
                "For security, the seeder will not grant Administrator to an existing account.");
        }

        var administrator = new IdentityUser
        {
            UserName = email,
            Email = email
        };

        IdentityResult createResult = await userManager.CreateAsync(administrator, password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                "Could not create administrator: " + DescribeErrors(createResult));

        IdentityResult roleResult = await userManager.AddToRoleAsync(
            administrator, RoleConstants.Administrator);

        if (!roleResult.Succeeded)
            throw new InvalidOperationException(
                "Administrator account was created but role assignment failed: " +
                DescribeErrors(roleResult));

        logger.LogInformation("Initial administrator account provisioned.");
    }

    private static string DescribeErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}

}
