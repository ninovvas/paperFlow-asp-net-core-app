
namespace PaperFlow.Web.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [AllowAnonymous]
    public sealed class AboutController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();
    }

}
