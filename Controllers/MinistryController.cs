using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Data;
using ClosedXML.Excel;
using System.IO;

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

        // إحصائيات وطنية
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

        // قائمة المستشفيات
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

        // قائمة مراكز التبرع
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

        // طلبات الدم الوطنية
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

        // Reports (PDF Mock)
        [HttpGet]
        public async Task<IActionResult> ExportReport(string type)
        {
            if (!IsMinistry()) return Unauthorized();

            var totalDonors = await _context.Donors.CountAsync();
            var totalHospitals = await _context.Hospitals.CountAsync();
            var totalUnits = await _context.BloodUnits.Where(b => b.Status == "Available").SumAsync(b => (int?)b.Quantity) ?? 0;
            var pendingReqs = await _context.Requests.CountAsync(r => r.Status == "Pending");

            // Mock PDF generation logic by returning a text file formatted like a report
            // In a real scenario, we would use iTextSharp or DinkToPdf here
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("===================================================");
            builder.AppendLine("           MINISTRY OF HEALTH REPORT               ");
            builder.AppendLine("           BLOOD BANK EGYPT SYSTEM                 ");
            builder.AppendLine("===================================================");
            builder.AppendLine($"Report Type: {type?.ToUpper()}");
            builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine("");
            builder.AppendLine("--- NATIONAL STATISTICS ---");
            builder.AppendLine($"Total Registered Donors: {totalDonors}");
            builder.AppendLine($"Total Active Hospitals: {totalHospitals}");
            builder.AppendLine($"Available Blood Units: {totalUnits}");
            builder.AppendLine($"Pending Blood Requests: {pendingReqs}");
            builder.AppendLine("");
            builder.AppendLine("===================================================");
            builder.AppendLine("This document is generated automatically by the system.");

            var fileBytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
            return File(fileBytes, "application/pdf", $"MinistryReport_{type}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Excel Report
        [HttpGet]
        public async Task<IActionResult> ExportReportExcel(string type)
        {
            if (!IsMinistry()) return Unauthorized();

            var donors    = await _context.Donors.ToListAsync();
            var hospitals = await _context.Hospitals.ToListAsync();
            var requests  = await _context.Requests.Include(r => r.Hospital).ToListAsync();
            var units     = await _context.BloodUnits.Where(b => b.Status == "Available").ToListAsync();

            using var workbook = new XLWorkbook();

            if (type == "governorates")
            {
                var ws = workbook.Worksheets.Add("Governorate Statistics");
                ws.Cell(1, 1).Value = "Governorate";
                ws.Cell(1, 2).Value = "Registered Donors";
                ws.Cell(1, 3).Value = "Total Requests";
                ws.Cell(1, 4).Value = "Completion %";

                var govGroups = donors.GroupBy(d => d.Governorate).ToList();
                int row = 2;
                foreach (var gov in govGroups)
                {
                    var govName    = gov.Key ?? "Unknown";
                    var govReqs    = requests.Where(r => r.Hospital != null && r.Hospital.City == govName).ToList();
                    var completed  = govReqs.Count(r => r.Status == "Approved" || r.Status == "Completed");
                    var pct        = govReqs.Count > 0 ? (int)Math.Round((double)completed / govReqs.Count * 100) : 100;

                    ws.Cell(row, 1).Value = govName;
                    ws.Cell(row, 2).Value = gov.Count();
                    ws.Cell(row, 3).Value = govReqs.Count;
                    ws.Cell(row, 4).Value = $"{pct}%";
                    row++;
                }
                ws.Columns().AdjustToContents();
            }
            else
            {
                var ws = workbook.Worksheets.Add("Ministry Report");
                ws.Cell(1, 1).Value = "Metric";
                ws.Cell(1, 2).Value = "Value";
                ws.Cell(2, 1).Value = "Total Donors";        ws.Cell(2, 2).Value = donors.Count;
                ws.Cell(3, 1).Value = "Total Hospitals";     ws.Cell(3, 2).Value = hospitals.Count;
                ws.Cell(4, 1).Value = "Available Units";     ws.Cell(4, 2).Value = units.Sum(u => u.Quantity);
                ws.Cell(5, 1).Value = "Pending Requests";    ws.Cell(5, 2).Value = requests.Count(r => r.Status == "Pending");
                ws.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"MinistryReport_{type}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
