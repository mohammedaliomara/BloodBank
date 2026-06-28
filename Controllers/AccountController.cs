using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Models;
using BloodBank.Data;

namespace BloodBank.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            // إذا كان المستخدم مسجلاً بالفعل، نوجهه للصفحة المناسبة
            var role = HttpContext.Session.GetString("bb_role");
            if (!string.IsNullOrEmpty(role))
                return RedirectToRole(role);

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // البحث عن الحساب بواسطة اسم المستخدم (Email) فقط
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == model.Username);

            if (account == null)
            {
                ModelState.AddModelError("", "بيانات الدخول غير صحيحة. تحقق من اسم المستخدم وكلمة المرور.");
                return View(model);
            }

            // التحقق من صحة التشفير
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<BloodBank.Models.Account>();
            var verificationResult = hasher.VerifyHashedPassword(account, account.Password, model.Password);

            if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "بيانات الدخول غير صحيحة. تحقق من اسم المستخدم وكلمة المرور.");
                return View(model);
            }

            // حفظ بيانات الجلسة على السيرفر (آمن، لا يعتمد على المتصفح)
            HttpContext.Session.SetString("bb_role", account.Role.ToLower());
            HttpContext.Session.SetInt32("bb_user_id", account.id);
            HttpContext.Session.SetString("bb_user_email", account.Email);

            return RedirectToRole(account.Role.ToLower());
        }

        // POST: /Account/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var vm = new LoginViewModel { Username = "" };
                ModelState.AddModelError("", "يرجى ملء جميع الحقول بشكل صحيح.");
                return View("Login", vm);
            }

            // Check email uniqueness
            var exists = await _context.Accounts.AnyAsync(a => a.Email == model.Email);
            if (exists)
            {
                ModelState.AddModelError("", "هذا البريد الإلكتروني مستخدم بالفعل.");
                return View("Login", new LoginViewModel { Username = model.Email });
            }

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Account>();
            var account = new Account
            {
                FirstName = model.FirstName,
                LastName  = model.LastName,
                Email     = model.Email,
                Role      = "donor",
                Password  = "temp"
            };
            account.Password = hasher.HashPassword(account, model.Password);

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // Create Donor profile
            var donor = new Donor
            {
                AccountId   = account.id,
                NationalId  = model.NationalId ?? "N/A",
                DateOfBirth = model.DateOfBirth,
                Gender      = model.Gender ?? "Male",
                BloodType   = model.BloodType ?? "O+",
                PhoneNumber = model.PhoneNumber ?? "",
                Governorate = model.Governorate ?? ""
            };
            _context.Donors.Add(donor);
            await _context.SaveChangesAsync();

            // Auto-login after registration
            HttpContext.Session.SetString("bb_role", "donor");
            HttpContext.Session.SetInt32("bb_user_id", account.id);
            HttpContext.Session.SetString("bb_user_email", account.Email);

            return RedirectToAction("Portal", "Donor");
        }

        // GET: /Account/Logout (للتوافق مع روابط الـ Views الحالية)
        [HttpGet]
        [ActionName("LogoutGet")]
        [Route("Account/Logout")]
        public IActionResult LogoutGet()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Helper: Redirect by Role
        private IActionResult RedirectToRole(string role)
        {
            return role switch
            {
                "admin"    => RedirectToAction("Dashboard", "Admin"),
                "donor"    => RedirectToAction("Portal", "Donor"),
                "hospital" => RedirectToAction("Portal", "Hospital"),
                "ministry" => RedirectToAction("Portal", "Ministry"),
                "staff"    => RedirectToAction("DonationCenter", "Donor"),
                _          => RedirectToAction("Login")
            };
        }
    }
}
