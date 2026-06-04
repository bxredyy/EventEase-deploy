using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    // POE Part 1B/2A/2B: Venue CRUD, Azurite image uploads, delete protection.
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobService _blobService;

        // POE Part 2A: only allow real image file types.
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedImageMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public VenuesController(ApplicationDbContext context, IBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();
            return View(venue);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    var mime = imageFile.ContentType.ToLowerInvariant();
                    if (!AllowedImageExtensions.Contains(ext) || !AllowedImageMimeTypes.Contains(mime))
                    {
                        ModelState.AddModelError("imageFile", "Only image files are allowed (JPG, JPEG, PNG, GIF, WebP).");
                        return View(venue);
                    }

                    try
                    {
                        venue.ImageUrl = await _blobService.UploadImageAsync(imageFile);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Image upload failed: {ex.Message} — make sure Azurite is running. Venue saved with placeholder image.";
                        venue.ImageUrl = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800";
                    }
                }
                else if (string.IsNullOrEmpty(venue.ImageUrl))
                {
                    venue.ImageUrl = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800";
                }

                _context.Add(venue);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Venue '{venue.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(venue);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();
            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue, IFormFile? imageFile)
        {
            if (id != venue.VenueId) return NotFound();

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    var mime = imageFile.ContentType.ToLowerInvariant();
                    if (!AllowedImageExtensions.Contains(ext) || !AllowedImageMimeTypes.Contains(mime))
                    {
                        ModelState.AddModelError("imageFile", "Only image files are allowed (JPG, JPEG, PNG, GIF, WebP).");
                        return View(venue);
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(venue.ImageUrl) &&
                            (venue.ImageUrl.Contains("127.0.0.1") || venue.ImageUrl.Contains("blob.core")))
                        {
                            await _blobService.DeleteImageAsync(venue.ImageUrl);
                        }
                        venue.ImageUrl = await _blobService.UploadImageAsync(imageFile);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Image upload failed: {ex.Message} — make sure Azurite is running. Other changes were saved.";
                    }
                }

                try
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                    if (!TempData.ContainsKey("Error"))
                        TempData["Success"] = $"Venue '{venue.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.VenueId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(venue);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();
            return View(venue);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();

            // POE Part 2B: block delete if active bookings exist.
            if (venue.Bookings.Any())
            {
                TempData["Error"] = $"Cannot delete '{venue.Name}' because it has {venue.Bookings.Count} active booking(s). Please remove those bookings first.";
                return RedirectToAction(nameof(Index));
            }

            // Remove the blob image first.
            if (!string.IsNullOrEmpty(venue.ImageUrl) &&
                (venue.ImageUrl.Contains("127.0.0.1") || venue.ImageUrl.Contains("blob.core")))
            {
                await _blobService.DeleteImageAsync(venue.ImageUrl);
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Venue '{venue.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool VenueExists(int id) => _context.Venues.Any(e => e.VenueId == id);
    }
}
