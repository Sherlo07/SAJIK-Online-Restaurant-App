using Microsoft.EntityFrameworkCore;

using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Filters;
using OnlineRestaurantApp.IRepository;
using OnlineRestaurantApp.Repository;

namespace OnlineRestaurantApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<OnlineRestaurantDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("cs"));
            });

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            builder.Services.AddSession();
            builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();



            builder.Services.AddScoped<ActionLogFilter>();
            builder.Services.AddScoped<GlobalExceptionFilter>();

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<ActionLogFilter>();
                options.Filters.Add<GlobalExceptionFilter>();
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseRouting();
            app.UseSession();

            app.UseAuthorization();


            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
