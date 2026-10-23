namespace PaperFlow.Web.Controllers
{

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using PaperFlow.Web.ViewModels.Account;

    [Authorize]
    public sealed class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [HttpGet("/Account/Profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null) return Challenge();
            return View(ToViewModel(user));
        }

        [HttpPost("/Account/Profile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            ModelState.Remove(nameof(ProfileViewModel.Email));
            model.Email = user.Email ?? string.Empty;
            model.PhoneNumber = model.PhoneNumber?.Trim();

            if (!ModelState.IsValid) return View(model);

            string? phone = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null : model.PhoneNumber;

            if (string.Equals(user.PhoneNumber, phone, StringComparison.Ordinal))
            {
                TempData["InfoMessage"] = "No changes to save.";
                return RedirectToAction(nameof(Profile));
            }

            var result = await userManager.SetPhoneNumberAsync(user, phone);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet("/Account/ChangePassword")]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null) return Challenge();
            if (!await userManager.HasPasswordAsync(user))
            {
                TempData["InfoMessage"] = "This account does not have a local password.";
                return RedirectToAction(nameof(Profile));
            }
            return View(new ChangePasswordViewModel());
        }

        [HttpPost("/Account/ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null) return Challenge();
            if (!await userManager.HasPasswordAsync(user))
            {
                TempData["InfoMessage"] = "This account does not have a local password.";
                return RedirectToAction(nameof(Profile));
            }
            if (!ModelState.IsValid) return View(model);

            var result = await userManager.ChangePasswordAsync(
                user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        private static ProfileViewModel ToViewModel(IdentityUser user) => new()
        {
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber
        };
    }

}


