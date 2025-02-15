using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;

namespace SchoolLibrary
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // �������� �� �������� ��� ������ �����
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ������������� �� Identity
            builder.Services.AddDefaultIdentity<LibraryUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false; // �������� false �� ������� �������
            })
            .AddRoles<IdentityRole>() // �������� �� ��������� �� ���� (Admin, Teacher, Student)
            .AddEntityFrameworkStores<ApplicationDbContext>();
      

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                DatabaseSeeder.SeedRolesAndAdminAsync(services).GetAwaiter().GetResult(); //��������� ����������� ����� ���������
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ���������� �� �������������� � �������������
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages(); // ������� ���������� �� Identity

            app.Run();

        }
    }

public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ������������ �� ApplicationDbContext � �������� ��� ������ �����
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // �������� �� MVC ��������
            services.AddControllersWithViews();

            // (����������) ������������� �� Identity ��� ��������� ������������/�����������
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ������������ �� middleware (error handling, static files, routing � �.�.)
        }

    }


}
