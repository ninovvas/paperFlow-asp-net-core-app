namespace PaperFlow.Web.Areas.Admin.Controllers
{

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using PaperFlow.Common.Constants;
    using PaperFlow.Services.Interfaces.Admin;
    using PaperFlow.Services.Models;


    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Administrator)]
    public sealed class UsersController(
    IAdminUserService adminUserService,
    UserManager<IdentityUser> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1,
            CancellationToken cancellationToken = default)
        {
            var model = await adminUserService.GetUsersAsync(search, page, cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string userId, string role, string? search, int page = 1,
            CancellationToken cancellationToken = default)
        {
            string? adminId = userManager.GetUserId(User);
            if (string.IsNullOrEmpty(adminId))
                return Challenge();

            var result = await adminUserService.SetRoleAsync(userId, role, adminId, cancellationToken);
            TempData[result == AdminRoleChangeResult.Success ? "AdminSuccess" : "AdminError"] = result switch
            {
                AdminRoleChangeResult.Success => "User role updated successfully.",
                AdminRoleChangeResult.NotFound => "The selected user was not found.",
                AdminRoleChangeResult.InvalidRole => "The selected role is not supported.",
                AdminRoleChangeResult.SelfDemotionForbidden => "You cannot remove your own Administrator role.",
                AdminRoleChangeResult.LastAdministratorForbidden => "At least one administrator must remain.",
                _ => "Could not update the role. Check the application log and try again."
            };

            return RedirectToAction(nameof(Index), new { search, page });
        }
    }

}
