using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Data;
using BloodBank.Models;

namespace BloodBank.Controllers
{
    public class HospitalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HospitalController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsHospital() =>
            HttpContext.Session.GetString("bb_role") == "hospital";

        // GET: /Hospital/Portal
        public async Task<IActionResult> Portal()
        {
            if (!IsHospital())
                return RedirectToAction("Login", "Account");

            var accountId = HttpContext.Session.GetInt32("bb_user_id") ?? 0;
            var hospital  = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.Email == HttpContext.Session.GetString("bb_email")
                                       || h.HospitalId == accountId);

            if (hospital == null)
                return RedirectToAction("Login", "Account");

            // إحصائيات لوحة المستشفى
            var activeRequests = await _context.Requests
                .CountAsync(r => r.HospitalId == hospital.HospitalId && r.Status == "Pending");

            var totalUnits = await _context.BloodUnits
                .Where(b => b.HospitalId == hospital.HospitalId && b.Status == "Available")
                .SumAsync(b => (int?)b.Quantity) ?? 0;

            var bloodInventory = await _context.BloodUnits
                .Where(b => b.HospitalId == hospital.HospitalId && b.Status == "Available")
                .GroupBy(b => b.BloodType)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(b => b.Quantity));

            var recentRequests = await _context.Requests
                .Where(r => r.HospitalId == hospital.HospitalId)
                .OrderByDescending(r => r.RequestDate)
                .Take(10)
                .ToListAsync();

            ViewBag.Hospital        = hospital;
            ViewBag.ActiveRequests  = activeRequests;
            ViewBag.TotalUnits      = totalUnits;
            ViewBag.BloodInventory  = bloodInventory;
            ViewBag.RecentRequests  = recentRequests;

            return View();
        }

        // ===================== طلب دم جديد =====================
        [HttpPost]
        public async Task<IActionResult> SubmitRequest(
            string bloodType, int quantity, string urgencyLevel,
            string? notes, DateTime neededAt)
        {
            if (!IsHospital()) return Unauthorized();

            // جلب معرف المستشفى من الجلسة عن طريق الـ Account
            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var hospital  = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.HospitalId == accountId);

            var hospitalId = hospital?.HospitalId ?? 0;

            _context.Requests.Add(new Request
            {
                HospitalId    = hospitalId,
                BloodType     = bloodType,
                QuantityNeeded = quantity,
                UrgencyLevel  = urgencyLevel,
                Status        = "Pending",
                Notes         = notes,
                RequestDate   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تقديم الطلب بنجاح" });
        }

        // ===================== طلباتنا =====================
        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            if (!IsHospital()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var hospital  = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.HospitalId == accountId);

            if (hospital == null) return Json(new List<object>());

            var requests = await _context.Requests
                .Where(r => r.HospitalId == hospital.HospitalId)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new {
                    r.RequestId,
                    r.BloodType,
                    r.QuantityNeeded,
                    r.UrgencyLevel,
                    r.Status,
                    r.RequestDate,
                    r.Notes
                })
                .ToListAsync();

            return Json(requests);
        }

        //  مخزون الدم 
        [HttpGet]
        public async Task<IActionResult> GetInventory()
        {
            if (!IsHospital()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var hospital  = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.HospitalId == accountId);

            if (hospital == null) return Json(new List<object>());

            var units = await _context.BloodUnits
                .Where(b => b.HospitalId == hospital.HospitalId && b.Status == "Available")
                .GroupBy(b => b.BloodType)
                .Select(g => new {
                    bloodType = g.Key,
                    total     = g.Sum(b => b.Quantity)
                })
                .ToListAsync();

            return Json(units);
        }

        // ===================== إحصائيات المستشفى =====================
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            if (!IsHospital()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var hospital = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.HospitalId == accountId);

            if (hospital == null) return Json(new { });

            var allRequests = await _context.Requests
                .Where(r => r.HospitalId == hospital.HospitalId)
                .ToListAsync();

            var thisMonth = allRequests.Where(r =>
                r.RequestDate.Month == DateTime.Now.Month &&
                r.RequestDate.Year == DateTime.Now.Year).ToList();

            var totalThisMonth = thisMonth.Count;
            var completedThisMonth = thisMonth.Count(r => r.Status == "Approved" || r.Status == "Completed");
            var completionRate = totalThisMonth > 0
                ? Math.Round((double)completedThisMonth / totalThisMonth * 100, 1)
                : 100.0;

            var totalUnitsUsed = allRequests
                .Where(r => r.Status == "Approved" || r.Status == "Completed")
                .Sum(r => r.QuantityNeeded);

            return Json(new
            {
                totalThisMonth,
                completedThisMonth,
                totalUnitsUsed,
                completionRate
            });
        }

        // ===================== Logout =====================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
