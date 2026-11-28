namespace PaperFlow.Web.Middleware;

/// <summary>
/// Adds defense-in-depth headers. This deliberately avoids a strict script-src
/// because the existing PaperFlow layout and statistics views use inline scripts.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Narrow CSP directives that work with the current Bootstrap/Chart.js UI.
        // A strict script-src requires moving inline scripts and reviewing CDN assets.
        headers["Content-Security-Policy"] =
            "frame-ancestors 'none'; object-src 'none'; base-uri 'self'";

        await next(context);
    }
}

