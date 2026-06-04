using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    // POE Part 1B: Full CRUD controller for Venues (Create, Read, Update, Delete)
    // POE Part 2A: Handles image uploads to Azurite via IBlobService
    // POE Part 2B: Blocks deletion of any venue that has active bookings
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobService _blobService;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedImageMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public VenuesController(ApplicationDbContext context, IBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: /Venues — the list page
        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        // GET: /Venues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // .Include() pulls the related Bookings in a single query
            // (LEFT JOIN under the hood) so the Details page can show
            // how many bookings this venue has
            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();
            return View(venue);
        }

        // GET: /Venues/Create — shows the empty form
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Venues/Create — handles form submission
        // [ValidateAntiForgeryToken] protects against CSRF attacks
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue, IFormFile? imageFile)
        {
            // ModelState.IsValid checks every Data Annotation on Venue
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

                // TempData survives ONE redirect — used to show the green
                // "Venue created successfully" banner on the Index page
                TempData["Success"] = $"Venue '{venue.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(venue);
        }

        // GET: /Venues/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();
            return View(venue);
        }

        // POST: /Venues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue, IFormFile? imageFile)
        {
            // Defensive check: the URL id must match the form's hidden id
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

        // GET: /Venues/Delete/5 — confirmation page.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();
            return View(venue);
        }

        // POST: /Venues/Delete/5 — actually removes the row.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();

            // POE Part 2B: HARD STOP — refuse to delete if any booking
            //              still references this venue. The user sees a
            //              clear red banner explaining why.
            if (venue.Bookings.Any())
            {
                TempData["Error"] = $"Cannot delete '{venue.Name}' because it has {venue.Bookings.Count} active booking(s). Please remove those bookings first.";
                return RedirectToAction(nameof(Index));
            }

            // Clean up the blob in Azurite before removing the DB row.
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

        // Tiny helper to check existence — used by the concurrency catch.
        private bool VenueExists(int id) => _context.Venues.Any(e => e.VenueId == id);
    }
}
