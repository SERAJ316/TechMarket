using Microsoft.EntityFrameworkCore;
using TechMarket.DataAccess.Data;
using TechMarket.DataAccess.Implementation;
using TechMarket.Entities.Repositories;
using Microsoft.AspNetCore.Identity;
using TechMarket.Utilities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;

namespace TechMarket.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.Configure<StripeData>(builder.Configuration.GetSection("stripe"));

            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(4);
            }).AddDefaultTokenProviders()
              .AddDefaultUI()
              .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddSingleton<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();

            var app = builder.Build();

            // Seed SuperAdmin
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await SeedSuperAdmin(services);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            StripeConfiguration.ApiKey = builder.Configuration.GetSection("stripe:Secretkey").Get<string>();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            // Routing for Razor Pages
            app.MapRazorPages();

            // Routing for Areas
            app.MapControllerRoute(
                name: "default",
                pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "Customer",
                pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        // Seed Super Admin Method
        private static async Task SeedSuperAdmin(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Create the SuperAdmin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync(SD.AdminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(SD.AdminRole));
            }

            // 2. Create the SuperAdmin user if it doesn't exist
            const string superAdminEmail = "admin@techmarket.com";
            const string superAdminPassword = "Admin@123"; 

            var existingUser = await userManager.FindByEmailAsync(superAdminEmail);
            if (existingUser == null)
            {
                var superAdmin = new IdentityUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
                if (result.Succeeded)
                {
                    // 3. Assign the SuperAdmin role to the user
                    await userManager.AddToRoleAsync(superAdmin, SD.AdminRole);
                }
            }
        }
    }
}
