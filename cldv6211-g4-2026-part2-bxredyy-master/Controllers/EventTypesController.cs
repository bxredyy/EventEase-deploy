using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    // POE Part 3A: CRUD for the EventType lookup table.
    public class EventTypesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var types = await _context.EventTypes
                .Include(et => et.Events)
                .OrderBy(et => et.Name)
                .ToListAsync();
            return View(types);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var eventType = await _context.EventTypes
                .Include(et => et.Events)
                .FirstOrDefaultAsync(et => et.EventTypeId == id);

            if (eventType == null) return NotFound();
            return View(eventType);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventType eventType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(eventType);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Event type '{eventType.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(eventType);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var eventType = await _context.EventTypes.FindAsync(id);
            if (eventType == null) return NotFound();
            return View(eventType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventType eventType)
        {
            if (id != eventType.EventTypeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventType);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Event type '{eventType.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.EventTypes.Any(et => et.EventTypeId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(eventType);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var eventType = await _context.EventTypes
                .Include(et => et.Events)
                .FirstOrDefaultAsync(et => et.EventTypeId == id);
            if (eventType == null) return NotFound();
            return View(eventType);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventType = await _context.EventTypes.FindAsync(id);
            if (eventType == null) return NotFound();

            _context.EventTypes.Remove(eventType);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Event type '{eventType.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
