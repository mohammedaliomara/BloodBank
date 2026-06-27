using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Data;

namespace BloodBank.Controllers
{
    public class MinistryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MinistryController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsMinistry() =>
            HttpContext.Session.GetString("bb_role") == "ministry";

        // GET: /Ministry/Portal
        public IActionResult Portal()
        {
            if (!IsMinistry())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // ===================== إحصائيات وطنية =====================
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            if (!IsMinistry()) return Unauthorized();

            var totalDonors    = await _context.Donors.CountAsync();
            var totalHospitals = await _context.Hospitals.CountAsync();
            var totalCenters   = await _context.BloodCenters.CountAsync();
            var totalUnits     = await _context.BloodUnits
                                     .Where(b => b.Status == "Available")
                                     .SumAsync(b => (int?)b.Quantity) ?? 0;
            var pendingReqs    = await _context.Requests.CountAsync(r => r.Status == "Pending");

            // المخزون بالفصائل
            var inventoryByType = await _context.BloodUnits
                .Where(b => b.Status == "Available")
                .GroupBy(b => b.BloodType)
                .Select(g => new { bloodType = g.Key, total = g.Sum(b => b.Quantity) })
                .ToListAsync();

            // المتبرعون حسب المحافظة
            var donorsByGov = await _context.Donors
                .GroupBy(d => d.Governorate ?? "غير محدد")
                .Select(g => new { gov = g.Key, count = g.Count() })
                .ToListAsync();

            // طلبات المستشفيات حسب المدينة
            var requestsByCity = await _context.Requests
                .Include(r => r.Hospital)
                .GroupBy(r => r.Hospital != null ? r.Hospital.City : "غير محدد")
                .Select(g => new {
                    city = g.Key,
                    totalRequests = g.Count(),
                    completedRequests = g.Count(r => r.Status == "Approved" || r.Status == "Completed")
                })
                .ToListAsync();

            // نسبة الإتمام الوطنية
            var totalReqs     = await _context.Requests.CountAsync();
            var completedReqs = await _context.Requests.CountAsync(r => r.Status == "Approved" || r.Status == "Completed");
            var completionRate = totalReqs > 0
                ? Math.Round((double)completedReqs / totalReqs * 100, 1)
                : 100.0;

            return Json(new
            {
                totalDonors,
                totalHospitals,
                totalCenters,
                totalUnits,
                pendingReqs,
                completionRate,
                inventoryByType,
                donorsByGov,
                requestsByCity
            });
        }

        // ===================== قائمة المستشفيات =====================
        [HttpGet]
        public async Task<IActionResult> GetHospitals()
        {
            if (!IsMinistry()) return Unauthorized();

            var hospitals = await _context.Hospitals
                .Select(h => new {
                    h.HospitalId,
                    h.Name,
                    h.Address,
                    h.City,
                    h.Phone,
                    h.Email,
                    h.Status,
                    h.CreatedAt
                })
                .OrderBy(h => h.Name)
                .ToListAsync();

            return Json(hospitals);
        }

        // ===================== قائمة مراكز التبرع =====================
        [HttpGet]
        public async Task<IActionResult> GetCenters()
        {
            if (!IsMinistry()) return Unauthorized();

            var centers = await _context.BloodCenters
                .Select(c => new {
                    c.Id,
                    c.Name,
                    c.Address,
                    c.Governorate,
                    c.PhoneNumber
                })
                .OrderBy(c => c.Governorate)
                .ToListAsync();

            return Json(centers);
        }

        // ===================== طلبات الدم الوطنية =====================
        [HttpGet]
        public async Task<IActionResult> GetNationalRequests(string? status)
        {
            if (!IsMinistry()) return Unauthorized();

            var query = _context.Requests.Include(r => r.Hospital).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new {
                    r.RequestId,
                    r.BloodType,
                    r.QuantityNeeded,
                    r.UrgencyLevel,
                    r.Status,
                    r.RequestDate,
                    HospitalName = r.Hospital != null ? r.Hospital.Name : "—",
                    HospitalCity = r.Hospital != null ? r.Hospital.City : "—"
                })
                .ToListAsync();

            return Json(requests);
        }

        // ===================== Logout =====================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
