namespace PaperFlow.Web.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using PaperFlow.Web.Models;
    using System.Diagnostics;

    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction(nameof(Dashboard));

            return View("Landing");
        }

        [Authorize]
        public IActionResult Dashboard() => View("Index");

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
