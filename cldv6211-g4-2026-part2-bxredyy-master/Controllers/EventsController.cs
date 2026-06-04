using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    // POE Part 1B: Full CRUD controller for Events
    // POE Part 2A: Image uploads to Azurite via IBlobService
    // POE Part 2B: Blocks deletion of any event that has active bookings
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobService _blobService;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedImageMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public EventsController(ApplicationDbContext context, IBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: /Events — the list page
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.ToListAsync();
            return View(events);
        }

        // GET: /Events/Details/5 — pulls related bookings in one query
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();
            return View(ev);
        }

        // GET: /Events/Create — empty form
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event ev, IFormFile? imageFile)
        {
            // Validate event dates
            if (ev.EndDate < ev.StartDate)
                ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    var mime = imageFile.ContentType.ToLowerInvariant();
                    if (!AllowedImageExtensions.Contains(ext) || !AllowedImageMimeTypes.Contains(mime))
                    {
                        ModelState.AddModelError("imageFile", "Only image files are allowed (JPG, JPEG, PNG, GIF, WebP).");
                        return View(ev);
                    }

                    try
                    {
                        ev.ImageUrl = await _blobService.UploadImageAsync(imageFile);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Image upload failed: {ex.Message} — make sure Azurite is running. Event saved with placeholder image.";
                        ev.ImageUrl = "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?w=800";
                    }
                }
                else if (string.IsNullOrEmpty(ev.ImageUrl))
                {
                    ev.ImageUrl = "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?w=800";
                }

                _context.Add(ev);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Event '{ev.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(ev);
        }

        // GET: /Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        // POST: /Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event ev, IFormFile? imageFile)
        {
            if (id != ev.EventId) return NotFound();

            if (ev.EndDate < ev.StartDate)
                ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    var mime = imageFile.ContentType.ToLowerInvariant();
                    if (!AllowedImageExtensions.Contains(ext) || !AllowedImageMimeTypes.Contains(mime))
                    {
                        ModelState.AddModelError("imageFile", "Only image files are allowed (JPG, JPEG, PNG, GIF, WebP).");
                        return View(ev);
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(ev.ImageUrl) &&
                            (ev.ImageUrl.Contains("127.0.0.1") || ev.ImageUrl.Contains("blob.core")))
                        {
                            await _blobService.DeleteImageAsync(ev.ImageUrl);
                        }
                        ev.ImageUrl = await _blobService.UploadImageAsync(imageFile);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Image upload failed: {ex.Message} — make sure Azurite is running. Other changes were saved.";
                    }
                }

                try
                {
                    _context.Update(ev);
                    await _context.SaveChangesAsync();
                    if (!TempData.ContainsKey("Error"))
                        TempData["Success"] = $"Event '{ev.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(ev.EventId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ev);
        }

        // GET: /Events/Delete/5 — confirmation page
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();
            return View(ev);
        }

        // POST: /Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();

            // POE Part 2B: Refuse to delete an event tied to active bookings
            if (ev.Bookings.Any())
            {
                TempData["Error"] = $"Cannot delete '{ev.Name}' because it has {ev.Bookings.Count} active booking(s). Please remove those bookings first.";
                return RedirectToAction(nameof(Index));
            }

            // Tidy up the blob before removing the DB row
            if (!string.IsNullOrEmpty(ev.ImageUrl) &&
                (ev.ImageUrl.Contains("127.0.0.1") || ev.ImageUrl.Contains("blob.core")))
            {
                await _blobService.DeleteImageAsync(ev.ImageUrl);
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Event '{ev.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id) => _context.Events.Any(e => e.EventId == id);
    }
}
