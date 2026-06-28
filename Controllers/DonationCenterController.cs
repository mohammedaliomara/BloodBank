using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Models;
using BloodBank.Data;

namespace BloodBank.Controllers
{
    public class DonationCenterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonationCenterController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsCenterStaff() =>
            HttpContext.Session.GetString("bb_role") == "staff" ||
            HttpContext.Session.GetString("bb_role") == "center";

        public async Task<IActionResult> Portal()
        {
            if (!IsCenterStaff()) return RedirectToAction("Login", "Account");

            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var account = await _context.Accounts.FindAsync(accountId);
            var centerId = account?.BloodCenterId ?? 0;
            var center = await _context.BloodCenters.FindAsync(centerId);

            var today = DateTime.Today;
            
            // Only fetch appointments for this specific center for today
            var appointments = await _context.Appointments
                .Include(a => a.Donor)
                .Include(a => a.Hospital)
                .Where(a => a.AppointmentDate.Date == today && a.BloodCenterId == centerId)
                .OrderBy(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.Center = center;
            ViewBag.CompletedToday = appointments.Count(a => a.Status == "Completed");
            ViewBag.RemainingToday = appointments.Count(a => a.Status != "Completed");

            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> CompleteDonation(int appointmentId)
        {
            if (!IsCenterStaff() && HttpContext.Session.GetString("bb_role") != "admin") return Unauthorized();

            var accountId = HttpContext.Session.GetInt32("bb_user_id");
            var account = await _context.Accounts.FindAsync(accountId);
            var centerId = account?.BloodCenterId ?? 0;

            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null || appointment.Status == "Completed") return NotFound();

            // Enforce staff can only complete appointments for their own center
            if (HttpContext.Session.GetString("bb_role") != "admin" && appointment.BloodCenterId != centerId)
                return Unauthorized();

            var donor = await _context.Donors.FindAsync(appointment.DonorId);
            if (donor == null) return NotFound();

            // Mark Appointment as Completed
            appointment.Status = "Completed";

            // Create Blood Unit
            var unit = new BloodUnit
            {
                BloodType = donor.BloodType,
                Quantity = 1,
                HospitalId = appointment.HospitalId, // Assigning to the location of the appointment
                CollectionDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(42),
                Status = "Available"
            };

            _context.BloodUnits.Add(unit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Donation completed. Blood unit created successfully with 42-day expiry." });
        }

        public async Task<IActionResult> Index(string searchString, string currentFilter, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            var centers = from c in _context.BloodCenters
                          where c.Status != "Inactive"
                          select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                centers = centers.Where(c => c.Name.Contains(searchString) || c.Governorate.Contains(searchString));
            }

            int pageSize = 10;
            return View(await PaginatedList<BloodCenter>.CreateAsync(centers.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bloodCenter = await _context.BloodCenters
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bloodCenter == null)
            {
                return NotFound();
            }

            return View(bloodCenter);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Address,Governorate,PhoneNumber")] BloodCenter bloodCenter)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bloodCenter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bloodCenter);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bloodCenter = await _context.BloodCenters.FindAsync(id);
            if (bloodCenter == null)
            {
                return NotFound();
            }
            return View(bloodCenter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Governorate,PhoneNumber")] BloodCenter bloodCenter)
        {
            if (id != bloodCenter.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bloodCenter);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BloodCenterExists(bloodCenter.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(bloodCenter);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bloodCenter = await _context.BloodCenters
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bloodCenter == null)
            {
                return NotFound();
            }

            return View(bloodCenter);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bloodCenter = await _context.BloodCenters.FindAsync(id);
            if (bloodCenter != null)
            {
                // Soft Delete
                bloodCenter.Status = "Inactive";
                _context.Update(bloodCenter);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BloodCenterExists(int id)
        {
            return _context.BloodCenters.Any(e => e.Id == id);
        }
    }
}
