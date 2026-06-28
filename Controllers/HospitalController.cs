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

        // طلب دم جديد
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

        // طلباتنا
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

        // استهلاك وحدة دم (Auto-Replenishment Trigger)
        [HttpPost]
        public async Task<IActionResult> ConsumeUnit(int bloodUnitId)
        {
            if (!IsHospital()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var hospital = await _context.Hospitals.FirstOrDefaultAsync(h => h.HospitalId == accountId);
            if (hospital == null) return Unauthorized();

            var unit = await _context.BloodUnits.FindAsync(bloodUnitId);
            if (unit == null || unit.HospitalId != hospital.HospitalId) return NotFound();

            unit.Status = "Consumed";
            
            // Business Rule: Auto-Replenishment Trigger
            var remainingUnits = await _context.BloodUnits
                .CountAsync(b => b.HospitalId == hospital.HospitalId && b.BloodType == unit.BloodType && b.Status == "Available");

            if (remainingUnits < 10) // Auto-replenishment threshold
            {
                // Only create if we don't already have a pending request for this type
                var existingRequest = await _context.Requests.AnyAsync(r => 
                    r.HospitalId == hospital.HospitalId && r.BloodType == unit.BloodType && r.Status == "Pending");
                
                if (!existingRequest)
                {
                    _context.Requests.Add(new Request
                    {
                        HospitalId = hospital.HospitalId,
                        BloodType = unit.BloodType,
                        QuantityNeeded = 20, // Standard auto-order size
                        UrgencyLevel = "Urgent",
                        Status = "Pending",
                        Notes = "Auto-generated stock replenishment request.",
                        RequestDate = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // إحصائيات المستشفى
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

        // تأكيد الاستلام (Delivery Confirmation)
        [HttpPost]
        public async Task<IActionResult> ConfirmReceipt(int requestId)
        {
            if (!IsHospital()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var hospital = await _context.Hospitals.FirstOrDefaultAsync(h => h.HospitalId == accountId);
            if (hospital == null) return Unauthorized();

            var request = await _context.Requests.FindAsync(requestId);
            if (request == null || request.HospitalId != hospital.HospitalId) return NotFound();

            if (request.Status != "Dispatched")
                return BadRequest(new { message = "Request is not in a dispatched state." });

            // Find all units tied to this hospital that are dispatched
            // Since we set unit.HospitalId when dispatching, we can find them
            var dispatchedUnits = await _context.BloodUnits
                .Where(b => b.HospitalId == hospital.HospitalId && b.Status == "Dispatched" && b.BloodType == request.BloodType)
                .ToListAsync();

            foreach (var unit in dispatchedUnits)
            {
                unit.Status = "Available"; // Now it's officially in hospital inventory
            }

            request.Status = "Completed";
            
            // Add to audit log for traceability
            var auditLog = new BloodBank.Models.AuditLog
            {
                Action = "Delivery Confirmed",
                EntityName = "Request",
                EntityId = request.RequestId,
                Details = $"Hospital {hospital.Name} confirmed receipt of {dispatchedUnits.Count} units of {request.BloodType}.",
                Timestamp = DateTime.UtcNow,
                UserId = $"Hospital_{hospital.HospitalId}"
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تأكيد الاستلام بنجاح" });
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
