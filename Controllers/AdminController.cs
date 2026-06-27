using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Data;
using BloodBank.Models;
using BloodBank.Services;

namespace BloodBank.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AdminController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("bb_role") == "admin";

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // إحصائيات للوحة التحكم
            ViewBag.TotalDonors     = await _context.Donors.CountAsync();
            ViewBag.TotalHospitals  = await _context.Hospitals.CountAsync();
            ViewBag.TotalBloodUnits = await _context.BloodUnits.Where(b => b.Status == "Available").SumAsync(b => (int?)b.Quantity) ?? 0;
            ViewBag.PendingRequests = await _context.Requests.CountAsync(r => r.Status == "Pending");
            ViewBag.TotalCenters    = await _context.BloodCenters.CountAsync();
            
            var today = DateTime.Today;
            var sevenDaysLater = today.AddDays(7);
            var threeDaysLater = today.AddDays(3);
            ViewBag.ExpiringSoon    = await _context.BloodUnits
                .CountAsync(b => b.Status == "Available" && b.ExpiryDate >= today && b.ExpiryDate <= sevenDaysLater);
            ViewBag.ExpiringSoon3   = await _context.BloodUnits
                .CountAsync(b => b.Status == "Available" && b.ExpiryDate >= today && b.ExpiryDate <= threeDaysLater);

            // آخر 5 مواعيد
            ViewBag.RecentAppointments = await _context.Appointments
                .Include(a => a.Hospital)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(4)
                .ToListAsync();

            // آخر 5 طلبات
            ViewBag.RecentRequests = await _context.Requests
                .Include(r => r.Hospital)
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ===================== Donors (JSON API للـ Dashboard) =====================
        [HttpGet]
        public async Task<IActionResult> GetDonors(string? bloodType, string? status)
        {
            if (!IsAdmin()) return Unauthorized();

            var query = _context.Donors.AsQueryable();

            if (!string.IsNullOrEmpty(bloodType))
                query = query.Where(d => d.BloodType == bloodType);

            var donors = await query
                .OrderByDescending(d => d.Id)
                .Select(d => new {
                    d.Id,
                    d.NationalId,
                    d.BloodType,
                    d.Governorate,
                    d.PhoneNumber,
                    d.Gender
                })
                .ToListAsync();

            return Json(donors);
        }

        [HttpGet]
        public async Task<IActionResult> ExportDonors()
        {
            if (!IsAdmin()) return Unauthorized();

            var donors = await _context.Donors.Include(d => d.Account).ToListAsync();
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("\uFEFFId,NationalId,Name,DateOfBirth,Gender,BloodType,PhoneNumber,Governorate,Email");

            foreach (var d in donors)
            {
                var name = d.Account != null ? $"{d.Account.FirstName} {d.Account.LastName}" : "Unknown";
                var email = d.Account != null ? d.Account.Email : "Unknown";
                builder.AppendLine($"{d.Id},{d.NationalId},{name},{d.DateOfBirth:yyyy-MM-dd},{d.Gender},{d.BloodType},{d.PhoneNumber},{d.Governorate},{email}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Donors.csv");
        }

        // ===================== Requests =====================
        [HttpGet]
        public async Task<IActionResult> GetRequests(string? status)
        {
            if (!IsAdmin()) return Unauthorized();

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
                    r.Notes,
                    HospitalName = r.Hospital != null ? r.Hospital.Name : "—"
                })
                .ToListAsync();

            return Json(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            if (!IsAdmin()) return Unauthorized();

            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.Status = "Approved";
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            if (!IsAdmin()) return Unauthorized();

            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.Status = "Rejected";
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ===================== Blood Inventory =====================
        [HttpGet]
        public async Task<IActionResult> GetInventory()
        {
            if (!IsAdmin()) return Unauthorized();

            var units = await _context.BloodUnits
                .Where(b => b.Status == "Available")
                .ToListAsync();

            var today = DateTime.Today;
            var grouped = units
                .GroupBy(b => b.BloodType)
                .Select(g => new {
                    bloodType     = g.Key,
                    total         = g.Sum(b => b.Quantity),
                    expiringSoon  = g.Count(b => (b.ExpiryDate.Date - today).Days <= 7),
                    expired       = g.Count(b => b.ExpiryDate.Date < today)
                })
                .ToList();

            return Json(grouped);
        }

        // ===================== Hospitals =====================
        [HttpGet]
        public async Task<IActionResult> GetHospitals()
        {
            if (!IsAdmin()) return Unauthorized();

            var hospitals = await _context.Hospitals
                .Select(h => new {
                    h.HospitalId,
                    h.Name,
                    h.Phone,
                    h.Address,
                    h.City,
                    h.Status
                })
                .ToListAsync();

            return Json(hospitals);
        }

        // ===================== Email Notifications (MailKit) =====================
        [HttpPost]
        public async Task<IActionResult> SendSMS(int donorId, string message, string bloodNeeded)
        {
            if (!IsAdmin()) return Unauthorized();

            var donor = await _context.Donors
                .Include(d => d.Account)
                .FirstOrDefaultAsync(d => d.Id == donorId);

            if (donor == null) return NotFound();

            // Save notification record
            _context.SMSNotifications.Add(new BloodBank.Models.SMSNotification
            {
                IdDonor     = donorId,
                Message     = message,
                BloodNeeded = bloodNeeded,
                SentAt      = DateTime.Now,
                IsRead      = false,
                IdAppoint   = null
            });
            await _context.SaveChangesAsync();

            // Send real email if donor has an account with email
            if (donor.Account != null && !string.IsNullOrEmpty(donor.Account.Email))
            {
                var donorName = $"{donor.Account.FirstName} {donor.Account.LastName}";
                var subject = $"🩸 Urgent Blood Request — {bloodNeeded} Needed";
                var htmlBody = BuildEmailTemplate(donorName, bloodNeeded, message);
                try
                {
                    await _emailService.SendEmailAsync(donor.Account.Email, donorName, subject, htmlBody);
                }
                catch (Exception ex)
                {
                    // Log but don't fail — notification is already saved
                    Console.WriteLine($"Email send failed: {ex.Message}");
                }
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SendBulkSMS(string message, string bloodNeeded, string governorate)
        {
            if (!IsAdmin()) return Unauthorized();

            var donors = await _context.Donors
                .Include(d => d.Account)
                .Where(d => string.IsNullOrEmpty(governorate) || d.Governorate == governorate)
                .ToListAsync();

            foreach (var donor in donors)
            {
                _context.SMSNotifications.Add(new BloodBank.Models.SMSNotification
                {
                    IdDonor     = donor.Id,
                    Message     = message,
                    BloodNeeded = bloodNeeded,
                    SentAt      = DateTime.Now,
                    IsRead      = false,
                    IdAppoint   = null
                });
            }
            await _context.SaveChangesAsync();

            // Send real emails to donors who have email accounts
            var emailRecipients = donors
                .Where(d => d.Account != null && !string.IsNullOrEmpty(d.Account.Email))
                .Select(d => (
                    Email: d.Account!.Email,
                    Name: $"{d.Account.FirstName} {d.Account.LastName}"
                ))
                .ToList();

            int emailsSent = 0;
            if (emailRecipients.Any())
            {
                var subject = $"🩸 Urgent Blood Request — {bloodNeeded} Needed";
                var htmlBody = BuildEmailTemplate("Donor", bloodNeeded, message);
                try
                {
                    emailsSent = await _emailService.SendBulkEmailAsync(emailRecipients, subject, htmlBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bulk email send failed: {ex.Message}");
                }
            }

            return Ok(new { Count = donors.Count, EmailsSent = emailsSent });
        }

        private static string BuildEmailTemplate(string donorName, string bloodNeeded, string message)
        {
            var css = @"
              body { font-family: 'Segoe UI', sans-serif; background: #f5f5f5; margin: 0; padding: 0; }
              .container { max-width: 560px; margin: 30px auto; background: #fff; border-radius: 14px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.1); }
              .header { background: linear-gradient(135deg, #C8102E, #8B0000); padding: 32px 28px; text-align: center; }
              .header h1 { color: #fff; font-size: 22px; margin: 0 0 6px; }
              .header p { color: rgba(255,255,255,0.7); font-size: 13px; margin: 0; }
              .blood-badge { display: inline-block; background: #fff; color: #C8102E; font-size: 36px; font-weight: 900; padding: 10px 28px; border-radius: 10px; margin: 20px 0 0; }
              .body { padding: 28px; }
              .body h2 { font-size: 18px; color: #1a1a2e; margin-bottom: 8px; }
              .body p { font-size: 14px; color: #555; line-height: 1.7; margin: 0 0 16px; }
              .cta { display: block; background: #C8102E; color: #fff; text-decoration: none; text-align: center; padding: 14px; border-radius: 10px; font-size: 15px; font-weight: 700; margin-top: 20px; }
              .footer { background: #f9f9f9; padding: 16px 28px; font-size: 11px; color: #999; text-align: center; border-top: 1px solid #eee; }";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><style>{css}</style></head>
<body>
  <div class=""container"">
    <div class=""header"">
      <h1>&#129656; BloodBank Egypt</h1>
      <p>National Blood Donation System</p>
      <div class=""blood-badge"">{bloodNeeded}</div>
    </div>
    <div class=""body"">
      <h2>Dear {donorName},</h2>
      <p>{message}</p>
      <p>Your blood type <strong>{bloodNeeded}</strong> is urgently needed. Your donation can save up to <strong>3 lives</strong>.</p>
      <a href=""https://donorblood.runasp.net"" class=""cta"">Donate Now &rarr;</a>
    </div>
    <div class=""footer"">BloodBank Egypt &middot; Sent automatically by the National Blood System &middot; Please do not reply to this email.</div>
  </div>
</body>
</html>";
        }

        // ===================== Admin Features =====================
        [HttpPost]
        public async Task<IActionResult> AssignUnit(int requestId)
        {
            if (!IsAdmin()) return Unauthorized();

            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();

            // Find matching available units
            var unitsToAssign = await _context.BloodUnits
                .Where(b => b.BloodType == request.BloodType && b.Status == "Available")
                .Take(request.QuantityNeeded)
                .ToListAsync();

            if (unitsToAssign.Count < request.QuantityNeeded)
                return BadRequest(new { message = "لا يوجد مخزون كافي" });

            foreach (var unit in unitsToAssign)
            {
                unit.Status = "Assigned";
                unit.HospitalId = request.HospitalId;
            }

            request.Status = "Assigned";
            await _context.SaveChangesAsync();
            return Ok();
        }



        [HttpGet]
        public async Task<IActionResult> GetCenters()
        {
            if (!IsAdmin()) return Unauthorized();
            var centers = await _context.BloodCenters.ToListAsync();
            return Json(centers);
        }

        [HttpPost]
        public async Task<IActionResult> AddHospital(string name, string address, string city, string phone, string email)
        {
            if (!IsAdmin()) return Unauthorized();
            var hospital = new Hospital
            {
                Name = name,
                Address = address,
                City = city,
                Phone = phone,
                Email = email,
                Status = "Active"
            };
            _context.Hospitals.Add(hospital);
            await _context.SaveChangesAsync();
            return Ok(hospital);
        }

        [HttpPost]
        public async Task<IActionResult> EditHospital(int id, string name, string address, string city, string phone, string email)
        {
            if (!IsAdmin()) return Unauthorized();
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) return NotFound();

            hospital.Name = name;
            hospital.Address = address;
            hospital.City = city;
            hospital.Phone = phone;
            hospital.Email = email;

            await _context.SaveChangesAsync();
            return Ok(hospital);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleHospitalStatus(int hospitalId)
        {
            if (!IsAdmin()) return Unauthorized();
            var hospital = await _context.Hospitals.FindAsync(hospitalId);
            if (hospital == null) return NotFound();

            hospital.Status = hospital.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
            return Ok(new { status = hospital.Status });
        }

        [HttpPost]
        public async Task<IActionResult> AddCenter(string name, string address, string governorate, string phone)
        {
            if (!IsAdmin()) return Unauthorized();
            var center = new BloodCenter
            {
                Name = name,
                Address = address,
                Governorate = governorate,
                PhoneNumber = phone,
                Status = "Active"
            };
            _context.BloodCenters.Add(center);
            await _context.SaveChangesAsync();
            return Ok(center);
        }

        [HttpPost]
        public async Task<IActionResult> EditCenter(int id, string name, string address, string governorate, string phone)
        {
            if (!IsAdmin()) return Unauthorized();
            var center = await _context.BloodCenters.FindAsync(id);
            if (center == null) return NotFound();

            center.Name = name;
            center.Address = address;
            center.Governorate = governorate;
            center.PhoneNumber = phone;

            await _context.SaveChangesAsync();
            return Ok(center);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCenterStatus(int centerId)
        {
            if (!IsAdmin()) return Unauthorized();
            var center = await _context.BloodCenters.FindAsync(centerId);
            if (center == null) return NotFound();

            center.Status = center.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
            return Ok(new { status = center.Status });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveDonor(int donorId)
        {
            if (!IsAdmin()) return Unauthorized();
            var donor = await _context.Donors.FindAsync(donorId);
            if (donor == null) return NotFound();
            
            // Assume we change a status. Donors don't have a status field in the original model, 
            // but we can simulate it if needed, or link their account.
            return Ok();
        }

        // ===================== Logout =====================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
