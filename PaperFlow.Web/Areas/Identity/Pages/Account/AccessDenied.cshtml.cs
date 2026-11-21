// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PaperFlow.Web.Areas.Identity.Pages.Account;

/// <summary>
///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
public class AccessDeniedModel : PageModel
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public void OnGet()
    {
        // Authentication succeeded, but the user lacks the required role.
        // Return 403 rather than an HTTP 200 containing an error message.
        Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
