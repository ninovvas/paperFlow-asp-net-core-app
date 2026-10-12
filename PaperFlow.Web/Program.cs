using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaperFlow.Data;

namespace PaperFlow.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ============================================
            // Database Configuration
            // ============================================

            var connectionString = builder.Configuration
                .GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<PowerFlowDbContext>(
                options =>
                    options.UseSqlServer(connectionString));

            builder.Services
                .AddDatabaseDeveloperPageExceptionFilter();

            // ============================================
            // ASP.NET Core Identity
            // ============================================

            builder.Services
                .AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                })
                .AddEntityFrameworkStores<PowerFlowDbContext>();

            // ============================================
            // MVC Configuration
            // ============================================

            builder.Services.AddControllersWithViews();

            // ============================================
            // Build Application
            // ============================================

            var app = builder.Build();

            // ============================================
            // Middleware Configuration
            // ============================================

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // ============================================
            // Static Files
            // ============================================

            app.MapStaticAssets();

            // ============================================
            // MVC Routing
            // ============================================

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            // ============================================
            // Identity Razor Pages
            // ============================================

            app.MapRazorPages()
                .WithStaticAssets();

            app.Run();
        }
    }
}
