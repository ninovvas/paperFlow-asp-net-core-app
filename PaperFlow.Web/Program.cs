
using Microsoft.AspNetCore.Identity;

using PaperFlow.Data;
using PaperFlow.Data.Extensions;
using PaperFlow.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// Database
// ==========================================

// Register PowerFlowDbContext from PaperFlow.Data.
// Do not register the DbContext a second time.

builder.Services.AddApplicationDbContext(
    builder.Configuration);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==========================================
// ASP.NET Core Identity
// ==========================================

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // User settings
        options.User.RequireUniqueEmail = true;

        // Password settings
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;

        // Development only, until email confirmation
        // has been configured.
        options.SignIn.RequireConfirmedAccount = false;

        // Account lockout settings
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<PowerFlowDbContext>();

// ==========================================
// PaperFlow Research Services
// ==========================================

builder.Services.AddPaperFlowServices();

// ==========================================
// MVC + Razor Pages
// ==========================================

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

// ==========================================
// Build application
// ==========================================

var app = builder.Build();

// ==========================================
// Search result caching
// ==========================================

builder.Services.AddMemoryCache();

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

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// Routes
// ==========================================

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
