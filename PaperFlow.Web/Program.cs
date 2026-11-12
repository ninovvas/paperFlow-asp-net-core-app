
using Microsoft.AspNetCore.Identity;
using PaperFlow.Data;
using PaperFlow.Data.Extensions;
using PaperFlow.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddApplicationDbContext(builder.Configuration);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;

        options.SignIn.RequireConfirmedAccount = false;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<PowerFlowDbContext>();

// Application services
builder.Services.AddPaperFlowServices();
builder.Services.AddMemoryCache();

// MVC and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Error handling
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTP pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
