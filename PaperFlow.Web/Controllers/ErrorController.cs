namespace PaperFlow.Web.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public sealed class ErrorController : Controller
    {
        [HttpGet("/Error/StatusCode")]
        public IActionResult StatusCodePage(int code)
        {
            // Only render a known error page for valid error status codes.
            if (code < 400 || code > 599)
                return NotFound();

            Response.StatusCode = code;
            return code switch
            {
                404 => View("NotFound"),
                500 => View("ServerError"),
                _ => View("HttpError", code)
            };
        }

        [HttpGet("/Error/ServerError")]
        public IActionResult ServerError()
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View("ServerError");
        }
    }
}

