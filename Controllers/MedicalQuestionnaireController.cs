using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBank.Models;
using BloodBank.Data;

namespace BloodBank.Controllers
{
    public class MedicalQuestionnaireController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicalQuestionnaireController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var questionnaires = await _context.MedicalQuestionnaires
                .Include(m => m.Donor)
                .ToListAsync();
            return View(questionnaires);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionnaire = await _context.MedicalQuestionnaires
                .Include(m => m.Donor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (questionnaire == null)
            {
                return NotFound();
            }

            return View(questionnaire);
        }

        public IActionResult Create()
        {
            ViewBag.DonorId = _context.Donors.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DonorId,AgeRange,WeightRange,HadFullMealInLast4Hours,HasUncontrolledBloodPressure,TakesBloodThinners,HasActiveInfectiousDisease,HasChronicHeartOrLungCondition,HadSurgeryOrMajorIllnessLast6Months,IsPregnantOrRecentlyPregnant,DonatedWholeBloodLast90Days,DonatedPlateletsLast14Days,HadBloodTransfusionLast12Months,TraveledToMalariaRiskAreaLast12Months,HadTattooOrPiercingLast6Months,HadCovidOrVaccineLast28Days")] MedicalQuestionnaire medicalQuestionnaire)
        {
            if (ModelState.IsValid)
            {
                _context.Add(medicalQuestionnaire);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DonorId = _context.Donors.ToList();
            return View(medicalQuestionnaire);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionnaire = await _context.MedicalQuestionnaires.FindAsync(id);
            if (questionnaire == null)
            {
                return NotFound();
            }

            ViewBag.DonorId = _context.Donors.ToList();
            return View(questionnaire);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DonorId,AgeRange,WeightRange,HadFullMealInLast4Hours,HasUncontrolledBloodPressure,TakesBloodThinners,HasActiveInfectiousDisease,HasChronicHeartOrLungCondition,HadSurgeryOrMajorIllnessLast6Months,IsPregnantOrRecentlyPregnant,DonatedWholeBloodLast90Days,DonatedPlateletsLast14Days,HadBloodTransfusionLast12Months,TraveledToMalariaRiskAreaLast12Months,HadTattooOrPiercingLast6Months,HadCovidOrVaccineLast28Days")] MedicalQuestionnaire medicalQuestionnaire)
        {
            if (id != medicalQuestionnaire.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicalQuestionnaire);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicalQuestionnaireExists(medicalQuestionnaire.Id))
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

            ViewBag.DonorId = _context.Donors.ToList();
            return View(medicalQuestionnaire);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionnaire = await _context.MedicalQuestionnaires
                .Include(m => m.Donor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (questionnaire == null)
            {
                return NotFound();
            }

            return View(questionnaire);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var questionnaire = await _context.MedicalQuestionnaires.FindAsync(id);
            if (questionnaire != null)
            {
                _context.MedicalQuestionnaires.Remove(questionnaire);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MedicalQuestionnaireExists(int id)
        {
            return _context.MedicalQuestionnaires.Any(e => e.Id == id);
        }
    }
}
