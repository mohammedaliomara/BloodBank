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

        public async Task<IActionResult> Index()
        {
            var centers = await _context.BloodCenters.ToListAsync();
            return View(centers);
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
                _context.BloodCenters.Remove(bloodCenter);
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
