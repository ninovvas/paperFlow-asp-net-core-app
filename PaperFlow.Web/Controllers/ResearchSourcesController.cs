
namespace PaperFlow.Web.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [AllowAnonymous]
    public sealed class ResearchSourcesController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();
    }

}
