using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Models;
using BloodBank.Data;

namespace BloodBank.Controllers
{
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonorController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsDonor() =>
            HttpContext.Session.GetString("bb_role") == "donor";

        private bool IsStaff() =>
            HttpContext.Session.GetString("bb_role") == "staff";

        // GET: /Donor/Portal
        public async Task<IActionResult> Portal()
        {
            if (!IsDonor())
                return RedirectToAction("Login", "Account");

            var accountId = HttpContext.Session.GetInt32("bb_user_id") ?? 0;

            // جلب بيانات المتبرع من قاعدة البيانات
            var donor = await _context.Donors
                .Include(d => d.Account)
                .FirstOrDefaultAsync(d => d.AccountId == accountId);

            if (donor == null)
                return RedirectToAction("Login", "Account");

            // بيانات الحساب
            var account = donor.Account;
            var fullName  = $"{account?.FirstName} {account?.LastName}".Trim();
            var initials  = (account?.FirstName?.Length > 0 ? account.FirstName[0].ToString() : "")
                          + (account?.LastName?.Length  > 0 ? account.LastName[0].ToString()  : "");

            ViewBag.FullName   = fullName;
            ViewBag.Initials   = initials.ToUpper();
            ViewBag.BloodType  = donor.BloodType;
            ViewBag.Governorate = donor.Governorate;
            ViewBag.Gender     = donor.Gender;
            ViewBag.DonorId    = donor.Id;
            ViewBag.Email      = account?.Email;
            ViewBag.Phone      = donor.PhoneNumber;
            ViewBag.NationalId = donor.NationalId;

            // المواعيد المكتملة = عدد التبرعات
            var completedAppointments = await _context.Appointments
                .CountAsync(a => a.DonorId == donor.Id && a.Status == "Completed");
            ViewBag.TotalDonations = completedAppointments;
            ViewBag.LivesSaved     = completedAppointments * 3;

            // آخر موعد مكتمل
            var lastAppointment = await _context.Appointments
                .Include(a => a.Hospital)
                .Where(a => a.DonorId == donor.Id && a.Status == "Completed")
                .OrderByDescending(a => a.AppointmentDate)
                .FirstOrDefaultAsync();
            ViewBag.LastAppointment = lastAppointment;

            // الموعد القادم
            var nextAppointment = await _context.Appointments
                .Include(a => a.Hospital)
                .Where(a => a.DonorId == donor.Id
                    && (a.Status == "Confirmed" || a.Status == "Pending")
                    && a.AppointmentDate >= DateTime.Today)
                .OrderBy(a => a.AppointmentDate)
                .FirstOrDefaultAsync();
            ViewBag.NextAppointment = nextAppointment;

            // حساب الأهلية (90 يوم من آخر تبرع)
            bool isEligible = true;
            int daysUntilEligible = 0;
            if (lastAppointment != null)
            {
                var daysSince = (DateTime.Today - lastAppointment.AppointmentDate).Days;
                if (daysSince < 90)
                {
                    isEligible = false;
                    daysUntilEligible = 90 - daysSince;
                }
            }
            ViewBag.IsEligible        = isEligible;
            ViewBag.DaysUntilEligible = daysUntilEligible;

            // الإشعارات غير المقروءة
            var unreadCount = await _context.SMSNotifications
                .CountAsync(s => s.IdDonor == donor.Id && !s.IsRead);
            ViewBag.UnreadNotifications = unreadCount;

            // مراكز التبرع المتاحة
            var centers = await _context.BloodCenters.ToListAsync();
            ViewBag.DonationCenters = centers;

            return View();
        }

        // GET: /Donor/DonationCenter (للـ Staff فقط)
        public IActionResult DonationCenter()
        {
            if (!IsStaff())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // ===================== JSON API: المواعيد =====================
        [HttpGet]
        public async Task<IActionResult> GetMyAppointments()
        {
            if (!IsDonor()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id") ?? 0;
            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.AccountId == accountId);
            if (donor == null) return Json(new List<object>());

            var appointments = await _context.Appointments
                .Include(a => a.Hospital)
                .Where(a => a.DonorId == donor.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new {
                    a.AppointmentId,
                    a.AppointmentDate,
                    a.AppointmentTime,
                    a.Status,
                    a.Notes,
                    HospitalName = a.Hospital != null ? a.Hospital.Name : "—",
                    HospitalCity = a.Hospital != null ? a.Hospital.City : "—"
                })
                .ToListAsync();

            return Json(appointments);
        }

        // ===================== JSON API: الإشعارات =====================
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            if (!IsDonor()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id") ?? 0;
            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.AccountId == accountId);
            if (donor == null) return Json(new List<object>());

            var notifications = await _context.SMSNotifications
                .Where(s => s.IdDonor == donor.Id)
                .OrderByDescending(s => s.SentAt)
                .Select(s => new {
                    s.IdSMS,
                    s.Message,
                    s.BloodNeeded,
                    s.SentAt,
                    s.IsRead
                })
                .ToListAsync();

            return Json(notifications);
        }

        // ===================== POST: حجز موعد =====================
        [HttpPost]
        public async Task<IActionResult> BookAppointment(int hospitalId, DateTime appointmentDate, string appointmentTime, string? notes)
        {
            if (!IsDonor()) return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id") ?? 0;
            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.AccountId == accountId);
            if (donor == null) return NotFound();

            // تحليل الوقت
            if (!TimeSpan.TryParse(appointmentTime, out var timeSpan))
                return BadRequest(new { message = "وقت غير صالح" });

            _context.Appointments.Add(new Appointment
            {
                DonorId         = donor.Id,
                HospitalId      = hospitalId,
                AppointmentDate = appointmentDate,
                AppointmentTime = timeSpan,
                Status          = "Pending",
                Notes           = notes,
                CreatedAt       = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم حجز الموعد بنجاح" });
        }

        // ===================== POST: تعيين الإشعار كمقروء =====================
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (!IsDonor()) return Unauthorized();

            var notif = await _context.SMSNotifications.FindAsync(id);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ===================== Logout =====================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // ===================== CRUD Endpoints =====================
        public async Task<IActionResult> Index()
        {
            var donors = await _context.Donors.Include(d => d.Account).ToListAsync();
            return View(donors);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var donor = await _context.Donors
                .Include(d => d.Account)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (donor == null) return NotFound();
            return View(donor);
        }

        public IActionResult Create()
        {
            ViewBag.AccountId = _context.Accounts.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NationalId,DateOfBirth,Gender,BloodType,PhoneNumber,Governorate,AccountId")] Donor donor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(donor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.AccountId = _context.Accounts.ToList();
            return View(donor);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var donor = await _context.Donors.FindAsync(id);
            if (donor == null) return NotFound();
            ViewBag.AccountId = _context.Accounts.ToList();
            return View(donor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NationalId,DateOfBirth,Gender,BloodType,PhoneNumber,Governorate,AccountId")] Donor donor)
        {
            if (id != donor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(donor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonorExists(donor.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.AccountId = _context.Accounts.ToList();
            return View(donor);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var donor = await _context.Donors
                .Include(d => d.Account)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (donor == null) return NotFound();
            return View(donor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donor = await _context.Donors.FindAsync(id);
            if (donor != null)
            {
                _context.Donors.Remove(donor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DonorExists(int id)
        {
            return _context.Donors.Any(e => e.Id == id);
        }
    }
}

