

namespace PaperFlow.Web.Tests.Security
{
    using Microsoft.AspNetCore.Http;
    using NUnit.Framework;
    using PaperFlow.Web.Middleware;


[TestFixture]
public sealed class SecurityHeadersMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_AddsExpectedSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.Headers["X-Content-Type-Options"].ToString(), Is.EqualTo("nosniff"));
            Assert.That(context.Response.Headers["X-Frame-Options"].ToString(), Is.EqualTo("DENY"));
            Assert.That(context.Response.Headers["Referrer-Policy"].ToString(), Is.EqualTo("strict-origin-when-cross-origin"));
            Assert.That(context.Response.Headers["Permissions-Policy"].ToString(), Does.Contain("camera=()"));
            Assert.That(context.Response.Headers["Content-Security-Policy"].ToString(), Does.Contain("frame-ancestors 'none'"));
        });
    }

    [Test]
    public async Task InvokeAsync_CallsNextMiddlewareExactlyOnce()
    {
        var context = new DefaultHttpContext();
        int calls = 0;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.That(calls, Is.EqualTo(1));
    }

    [TestCase("/Admin/Users")]
    [TestCase("/PaperSearch")]
    [TestCase("/Identity/Account/Login")]
    public async Task InvokeAsync_AppliesHeadersRegardlessOfPath(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.Headers["X-Content-Type-Options"].ToString(), Is.EqualTo("nosniff"));
    }
}
    }

