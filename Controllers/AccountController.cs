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

        // GET: /Account/Logout (للتوافق مع روابط الـ Views الحالية)
        [HttpGet]
        [ActionName("LogoutGet")]
        [Route("Account/Logout")]
        public IActionResult LogoutGet()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ========================
        // Helper: Redirect by Role
        // ========================
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
