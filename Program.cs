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

            // Services
            builder.Services.AddControllersWithViews();

            // Email Service (MailKit)
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Background Services
            builder.Services.AddHostedService<BloodExpiryService>();

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
                    
                    // Manually create AuditLogs table if EnsureCreated was previously run without it
                    db.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' and xtype='U')
                        BEGIN
                            CREATE TABLE AuditLogs (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                Action NVARCHAR(MAX) NOT NULL,
                                EntityName NVARCHAR(MAX) NOT NULL,
                                EntityId INT NOT NULL,
                                Details NVARCHAR(MAX) NOT NULL,
                                Timestamp DATETIME2 NOT NULL,
                                UserId NVARCHAR(MAX) NOT NULL
                            )
                        END

                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'BloodCenterId' AND Object_ID = Object_ID(N'Accounts'))
                        BEGIN
                            ALTER TABLE Accounts ADD BloodCenterId int NULL;
                        END

                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'BloodCenterId' AND Object_ID = Object_ID(N'Appointments'))
                        BEGIN
                            ALTER TABLE Appointments ADD BloodCenterId int NULL;
                        END

                        IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Status' AND Object_ID = Object_ID(N'Donors'))
                        BEGIN
                            ALTER TABLE Donors ADD Status nvarchar(max) NOT NULL DEFAULT 'Pending';
                            ALTER TABLE Donors ADD RegistrationDate datetime2 NOT NULL DEFAULT GETUTCDATE();
                            ALTER TABLE Donors ADD RejectionReason nvarchar(max) NULL;
                            ALTER TABLE Donors ADD FullAddress nvarchar(max) NULL;
                            ALTER TABLE Donors ADD Weight float NULL;
                        END
                    ");

                    BloodBank.Data.DbSeeder.Seed(db);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Database initialization failed — app will still start.");
                }
            }

            // Middleware Pipeline
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
