using BloodBank.Data;
using BloodBank.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BloodBank
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== Services =====
            builder.Services.AddControllersWithViews();

            // Email Service (MailKit)
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddScoped<IEmailService, EmailService>();

            // قاعدة البيانات — تم التبديل لـ SQL Server
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ✅ حفظ مفاتيح التشفير في ملفات لضمان عدم فقدان الجلسة عند إعادة تشغيل السيرفر (App Pool Recycle)
            var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
                .SetApplicationName("BloodBankApp");

            // ✅ تسجيل الجلسات (Sessions) — ضروري لنظام تسجيل الدخول
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout        = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly    = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            });

            var app = builder.Build();

            // ✅ ملء قاعدة البيانات ببيانات تجريبية عند بدء التشغيل
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<BloodBank.Data.ApplicationDbContext>();
                    db.Database.EnsureCreated();
                    BloodBank.Data.DbSeeder.Seed(db);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Database initialization failed — app will still start.");
                }
            }

            // ===== Middleware Pipeline =====
            // ✅ runasp.net بيعمل SSL Termination على الـ proxy
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);

            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.UseRouting();

            // ✅ تفعيل الجلسات قبل Authorization
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
