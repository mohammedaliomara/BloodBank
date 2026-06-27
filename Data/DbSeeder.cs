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

            // =====================================================
            // 1. حسابات المستخدمين (Accounts)
            // =====================================================
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

            // =====================================================
            // 2. مستشفيات
            // =====================================================
            var h1 = new Hospital { Name = "مستشفى الشيخ زايد", Address = "القاهرة الجديدة، حي الشيخ زايد", City = "القاهرة",     Phone = "0224567890", Email = "hospital@bloodlink.org", Status = "Active" };
            var h2 = new Hospital { Name = "مستشفى دار الشفاء", Address = "المهندسين، الجيزة",               City = "الجيزة",       Phone = "0223456789", Email = "darshifa@bloodlink.org",  Status = "Active" };
            var h3 = new Hospital { Name = "مستشفى الرحمة",     Address = "الإسكندرية، محطة الرمل",          City = "الإسكندرية",   Phone = "0234561234", Email = "rahma@bloodlink.org",      Status = "Active" };
            db.Hospitals.AddRange(h1, h2, h3);
            db.SaveChanges();

            // =====================================================
            // 3. مراكز التبرع
            // =====================================================
            db.BloodCenters.AddRange(
                new BloodCenter { Name = "مركز تبرع المعادي",    Address = "شارع النيل، المعادي",          Governorate = "القاهرة",    PhoneNumber = "0221234567" },
                new BloodCenter { Name = "مركز تبرع المهندسين",  Address = "شارع جامعة الدول، المهندسين", Governorate = "الجيزة",     PhoneNumber = "0229876543" },
                new BloodCenter { Name = "مركز تبرع الإسكندرية", Address = "كورنيش الإسكندرية",            Governorate = "الإسكندرية", PhoneNumber = "0234001122" }
            );
            db.SaveChanges();

            // =====================================================
            // 4. متبرعون  (كل واحد له AccountId مختلف)
            // =====================================================
            var d1 = new Donor { NationalId = "29001010101010", DateOfBirth = new DateTime(1990,  1,  1), Gender = "Male",   BloodType = "O+",  PhoneNumber = "01001234567", Governorate = "القاهرة",    AccountId = donor1.id };
            var d2 = new Donor { NationalId = "29501010102020", DateOfBirth = new DateTime(1995,  5, 15), Gender = "Female", BloodType = "A+",  PhoneNumber = "01112345678", Governorate = "الجيزة",     AccountId = staff.id  };
            var d3 = new Donor { NationalId = "29801010103030", DateOfBirth = new DateTime(1998,  3, 22), Gender = "Male",   BloodType = "B-",  PhoneNumber = "01223456789", Governorate = "الإسكندرية", AccountId = hospital.id };
            var d4 = new Donor { NationalId = "29201010104040", DateOfBirth = new DateTime(1992,  7,  8), Gender = "Female", BloodType = "AB+", PhoneNumber = "01534567890", Governorate = "القاهرة",    AccountId = ministry.id };
            var d5 = new Donor { NationalId = "29601010105050", DateOfBirth = new DateTime(1996, 11,  3), Gender = "Male",   BloodType = "O-",  PhoneNumber = "01045678901", Governorate = "الجيزة",     AccountId = admin.id  };
            db.Donors.AddRange(d1, d2, d3, d4, d5);
            db.SaveChanges();

            // =====================================================
            // 5. وحدات الدم
            // =====================================================
            db.BloodUnits.AddRange(
                new BloodUnit { BloodType = "A+",  Quantity = 85,  HospitalId = h1.HospitalId, CollectionDate = DateTime.Today.AddDays(-10), ExpiryDate = DateTime.Today.AddDays(35), Status = "Available" },
                new BloodUnit { BloodType = "A-",  Quantity = 23,  HospitalId = h1.HospitalId, CollectionDate = DateTime.Today.AddDays(-5),  ExpiryDate = DateTime.Today.AddDays(40), Status = "Available" },
                new BloodUnit { BloodType = "B+",  Quantity = 8,   HospitalId = h1.HospitalId, CollectionDate = DateTime.Today.AddDays(-20), ExpiryDate = DateTime.Today.AddDays(5),  Status = "Available" },
                new BloodUnit { BloodType = "B-",  Quantity = 41,  HospitalId = h2.HospitalId, CollectionDate = DateTime.Today.AddDays(-8),  ExpiryDate = DateTime.Today.AddDays(28), Status = "Available" },
                new BloodUnit { BloodType = "O+",  Quantity = 72,  HospitalId = h2.HospitalId, CollectionDate = DateTime.Today.AddDays(-3),  ExpiryDate = DateTime.Today.AddDays(42), Status = "Available" },
                new BloodUnit { BloodType = "O-",  Quantity = 5,   HospitalId = h1.HospitalId, CollectionDate = DateTime.Today.AddDays(-25), ExpiryDate = DateTime.Today.AddDays(3),  Status = "Available" },
                new BloodUnit { BloodType = "AB+", Quantity = 120, HospitalId = h3.HospitalId, CollectionDate = DateTime.Today.AddDays(-2),  ExpiryDate = DateTime.Today.AddDays(45), Status = "Available" },
                new BloodUnit { BloodType = "AB-", Quantity = 34,  HospitalId = h3.HospitalId, CollectionDate = DateTime.Today.AddDays(-6),  ExpiryDate = DateTime.Today.AddDays(30), Status = "Available" }
            );
            db.SaveChanges();

            // =====================================================
            // 6. طلبات الدم
            // =====================================================
            db.Requests.AddRange(
                new Request { HospitalId = h1.HospitalId, BloodType = "O-",  QuantityNeeded = 3, UrgencyLevel = "Critical", Status = "Pending",  RequestDate = DateTime.UtcNow.AddHours(-2), Notes = "جراحة قلب مفتوح — حالة طارئة" },
                new Request { HospitalId = h1.HospitalId, BloodType = "A+",  QuantityNeeded = 2, UrgencyLevel = "Normal",   Status = "Approved", RequestDate = DateTime.UtcNow.AddDays(-1),  Notes = null },
                new Request { HospitalId = h2.HospitalId, BloodType = "B+",  QuantityNeeded = 4, UrgencyLevel = "Urgent",   Status = "Pending",  RequestDate = DateTime.UtcNow.AddHours(-5), Notes = "حادثة مرورية" },
                new Request { HospitalId = h3.HospitalId, BloodType = "AB+", QuantityNeeded = 1, UrgencyLevel = "Normal",   Status = "Approved", RequestDate = DateTime.UtcNow.AddDays(-3),  Notes = null },
                new Request { HospitalId = h2.HospitalId, BloodType = "O+",  QuantityNeeded = 5, UrgencyLevel = "Urgent",   Status = "Pending",  RequestDate = DateTime.UtcNow.AddHours(-1), Notes = "عملية استئصال" }
            );
            db.SaveChanges();

            // =====================================================
            // 7. المواعيد
            // =====================================================
            db.Appointments.AddRange(
                new Appointment { DonorId = d1.Id, HospitalId = h1.HospitalId, AppointmentDate = DateTime.Today.AddDays(2),  AppointmentTime = new TimeSpan(9, 0, 0),   Status = "Confirmed", Notes = "موعد تبرع دوري" },
                new Appointment { DonorId = d2.Id, HospitalId = h2.HospitalId, AppointmentDate = DateTime.Today.AddDays(3),  AppointmentTime = new TimeSpan(10, 30, 0), Status = "Pending",   Notes = null },
                new Appointment { DonorId = d3.Id, HospitalId = h1.HospitalId, AppointmentDate = DateTime.Today.AddDays(5),  AppointmentTime = new TimeSpan(11, 0, 0),  Status = "Pending",   Notes = "أول تبرع" },
                new Appointment { DonorId = d4.Id, HospitalId = h3.HospitalId, AppointmentDate = DateTime.Today.AddDays(-1), AppointmentTime = new TimeSpan(14, 0, 0),  Status = "Completed", Notes = "تم التبرع بنجاح" },
                new Appointment { DonorId = d5.Id, HospitalId = h2.HospitalId, AppointmentDate = DateTime.Today.AddDays(7),  AppointmentTime = new TimeSpan(9, 30, 0),  Status = "Confirmed", Notes = null }
            );
            db.SaveChanges();

            // =====================================================
            // 8. رسائل SMS
            // =====================================================
            db.SMSNotifications.AddRange(
                new SMSNotification { IdDonor = d1.Id, BloodNeeded = "O-", Message = "عاجل: نحتاج متبرعين بفصيلة O- في مستشفى الشيخ زايد. تواصل معنا على الفور.", SentAt = DateTime.Now.AddHours(-3), IsRead = false },
                new SMSNotification { IdDonor = d2.Id, BloodNeeded = "A+", Message = "شكراً لتبرعك السابق! لديك موعد تبرع قادم.",                                  SentAt = DateTime.Now.AddDays(-1),  IsRead = true  },
                new SMSNotification { IdDonor = d5.Id, BloodNeeded = "O-", Message = "عاجل: نحتاج متبرعين بفصيلة O- في مستشفى الشيخ زايد. تواصل معنا على الفور.", SentAt = DateTime.Now.AddHours(-3), IsRead = false }
            );
            db.SaveChanges();
        }
    }
}

