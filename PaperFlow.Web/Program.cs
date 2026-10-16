
using Microsoft.AspNetCore.Identity;

using PaperFlow.Data;
using PaperFlow.Data.Extensions;

using PaperFlow.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// Database
// ==========================================

builder.Services.AddApplicationDbContext(
    builder.Configuration);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==========================================
// ASP.NET Core Identity
// ==========================================

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;

        // Only for local development until
        // email confirmation is configured.
        options.SignIn.RequireConfirmedAccount = false;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<PowerFlowDbContext>();

// ==========================================
// Research API and Import Services
// ==========================================

builder.Services.AddPaperFlowServices();

// ==========================================
// MVC
// ==========================================

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==========================================
// Error handling
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ==========================================
// Middleware
// ==========================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
