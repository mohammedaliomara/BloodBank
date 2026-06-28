using BloodBank.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodBank.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            // لا تعيد الـ Seed لو البيانات موجودة بالفعل
            if (db.Accounts.Any()) return;

            // 1. حسابات المستخدمين (Accounts)
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Account>();

            var admin    = new Account { FirstName = "Admin",    LastName = "System",   Email = "admin@bloodlink.org",    Password = "demo1234", Role = "admin"    };
            var donor1   = new Account { FirstName = "Ahmed",    LastName = "Hassan",   Email = "donor@bloodlink.org",    Password = "demo1234", Role = "donor"    };
            var hospital = new Account { FirstName = "Zeina",    LastName = "Hospital", Email = "hospital@bloodlink.org", Password = "demo1234", Role = "hospital" };
            var ministry = new Account { FirstName = "Ministry", LastName = "Egypt",    Email = "ministry@bloodlink.org", Password = "demo1234", Role = "ministry" };
            var staff    = new Account { FirstName = "Nour",     LastName = "Staff",    Email = "staff@bloodlink.org",    Password = "demo1234", Role = "staff"    };

            foreach (var acc in new[] { admin, donor1, hospital, ministry, staff })
                acc.Password = hasher.HashPassword(acc, acc.Password);

            db.Accounts.AddRange(admin, donor1, hospital, ministry, staff);
            db.SaveChanges();

            // 2. مستشفيات
            var h1 = new Hospital { Name = "مستشفى الشيخ زايد", Address = "القاهرة الجديدة، حي الشيخ زايد", City = "القاهرة",     Phone = "0224567890", Email = "hospital@bloodlink.org", Status = "Active" };
            var h2 = new Hospital { Name = "مستشفى دار الشفاء", Address = "المهندسين، الجيزة",               City = "الجيزة",       Phone = "0223456789", Email = "darshifa@bloodlink.org",  Status = "Active" };
            var h3 = new Hospital { Name = "مستشفى الرحمة",     Address = "الإسكندرية، محطة الرمل",          City = "الإسكندرية",   Phone = "0234561234", Email = "rahma@bloodlink.org",      Status = "Active" };
            db.Hospitals.AddRange(h1, h2, h3);
            db.SaveChanges();

            // 3. مراكز التبرع
            db.BloodCenters.AddRange(
                new BloodCenter { Name = "مركز تبرع المعادي",    Address = "شارع النيل، المعادي",          Governorate = "القاهرة",    PhoneNumber = "0221234567" },
                new BloodCenter { Name = "مركز تبرع المهندسين",  Address = "شارع جامعة الدول، المهندسين", Governorate = "الجيزة",     PhoneNumber = "0229876543" },
                new BloodCenter { Name = "مركز تبرع الإسكندرية", Address = "كورنيش الإسكندرية",            Governorate = "الإسكندرية", PhoneNumber = "0234001122" }
            );
            db.SaveChanges();

            // تم إزالة البيانات الوهمية (المتطوعين، المواعيد، وحدات الدم، والطلبات) 
            // بناءً على طلبك لتكون قاعدة البيانات نظيفة للاستخدام الحقيقي.
        }
    }
}

