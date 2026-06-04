using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    // POE Part 1B: Full CRUD controller for Bookings
    // POE Part 2B: Prevents double bookings and shows
    //              clear error messages when validation fails
    // POE Part 2C: Provides the consolidated Search view that joins
    //              Booking + Venue + Event into one screen
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Bookings... list ALL bookings with venue + event names included
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .ToListAsync();
            return View(bookings);
        }

        // GET: /Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // GET: /Bookings/Create — pre-populates the dropdowns.
        public async Task<IActionResult> Create()
        {
            var viewModel = new BookingFormViewModel
            {
                // Sensible defaults so the date pickers aren't blank.
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                Venues = await GetVenueSelectList(),
                Events = await GetEventSelectList()
            };
            return View(viewModel);
        }

        // POST: /Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // POE Part 2B: Sanity check — end date can't be before start.
                if (viewModel.EndDate < viewModel.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                // POE Part 2B: DOUBLE-BOOKING PREVENTION.
                // Two date ranges overlap if: A.start < B.end AND A.end > B.start.
                // We check this against every existing booking for the SAME venue.
                // If anything overlaps, we refuse the booking and tell the user.
                bool hasConflict = await _context.Bookings.AnyAsync(b =>
                    b.VenueId == viewModel.VenueId &&
                    b.StartDate < viewModel.EndDate &&
                    b.EndDate > viewModel.StartDate);

                if (hasConflict)
                {
                    TempData["Error"] = "This venue is already booked for the selected date range. Please choose different dates or a different venue.";
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                // Map the ViewModel onto the actual entity, then save.
                // The booking reference is auto-generated (POE brief: "unique booking IDs").
                var booking = new Booking
                {
                    VenueId = viewModel.VenueId,
                    EventId = viewModel.EventId,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    BookingReference = GenerateBookingReference()
                };

                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Booking created successfully! Reference: {booking.BookingReference}";
                return RedirectToAction(nameof(Index));
            }

            // ModelState was invalid — re-show the form with the dropdowns repopulated.
            viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
            viewModel.Events = await GetEventSelectList(viewModel.EventId);
            return View(viewModel);
        }

        // GET: /Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            // Project the entity into the form ViewModel.
            var viewModel = new BookingFormViewModel
            {
                BookingId = booking.BookingId,
                VenueId = booking.VenueId,
                EventId = booking.EventId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                BookingReference = booking.BookingReference,
                Venues = await GetVenueSelectList(booking.VenueId),
                Events = await GetEventSelectList(booking.EventId)
            };
            return View(viewModel);
        }

        // POST: /Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookingFormViewModel viewModel)
        {
            if (id != viewModel.BookingId) return NotFound();

            if (ModelState.IsValid)
            {
                if (viewModel.EndDate < viewModel.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                // POE Part 2B: Same overlap check — but we EXCLUDE this booking
                //              from the conflict search (b.BookingId != ...),
                //              otherwise editing a booking would always
                //              "conflict" with itself
                bool hasConflict = await _context.Bookings.AnyAsync(b =>
                    b.VenueId == viewModel.VenueId &&
                    b.BookingId != viewModel.BookingId &&
                    b.StartDate < viewModel.EndDate &&
                    b.EndDate > viewModel.StartDate);

                if (hasConflict)
                {
                    TempData["Error"] = "This venue is already booked for the selected date range. Please choose different dates or a different venue.";
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                try
                {
                    var booking = await _context.Bookings.FindAsync(viewModel.BookingId);
                    if (booking == null) return NotFound();

                    booking.VenueId = viewModel.VenueId;
                    booking.EventId = viewModel.EventId;
                    booking.StartDate = viewModel.StartDate;
                    booking.EndDate = viewModel.EndDate;

                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Booking '{booking.BookingReference}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(viewModel.BookingId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
            viewModel.Events = await GetEventSelectList(viewModel.EventId);
            return View(viewModel);
        }

        // GET: /Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        // POST: /Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Booking deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POE Part 2C: The CONSOLIDATED booking view + simple search.
        //              Booking specialists can filter by booking ID,
        //              booking reference, event name, OR venue name.
        public async Task<IActionResult> Search(string? searchTerm)
        {
            var query = _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // .ToLower() makes the search case-insensitive 
                // .
                var term = searchTerm.Trim().ToLower();
                query = query.Where(b =>
                    b.BookingId.ToString().Contains(term) ||
                    b.BookingReference.ToLower().Contains(term) ||
                    b.Event!.Name.ToLower().Contains(term) ||
                    b.Venue!.Name.ToLower().Contains(term));
            }

            var results = await query.ToListAsync();

            // Project into the flat display ViewModel — keeps the .cshtml
            // simple (no need to access nested b.Venue.Name in the view)
            var viewModel = new BookingSearchViewModel
            {
                SearchTerm = searchTerm,
                Bookings = results.Select(b => new BookingDisplayViewModel
                {
                    BookingId = b.BookingId,
                    BookingReference = b.BookingReference,
                    VenueName = b.Venue?.Name ?? "Unknown",
                    VenueLocation = b.Venue?.Location ?? "Unknown",
                    VenueCapacity = b.Venue?.Capacity ?? 0,
                    EventName = b.Event?.Name ?? "Unknown",
                    EventDescription = b.Event?.Description,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate
                }).ToList()
            };

            return View(viewModel);
        }

        

        // Builds the Venue dropdown for the booking form. The "Selected"
        // flag pre-selects whatever the user previously chose (used on
        // re-display after a validation error or when editing)
        private async Task<IEnumerable<SelectListItem>> GetVenueSelectList(int selectedId = 0)
        {
            return (await _context.Venues.OrderBy(v => v.Name).ToListAsync())
                .Select(v => new SelectListItem
                {
                    Value = v.VenueId.ToString(),
                    Text = $"{v.Name} (Capacity: {v.Capacity:N0})",
                    Selected = v.VenueId == selectedId
                });
        }

        private async Task<IEnumerable<SelectListItem>> GetEventSelectList(int selectedId = 0)
        {
            return (await _context.Events.OrderBy(e => e.Name).ToListAsync())
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.Name,
                    Selected = e.EventId == selectedId
                });
        }

        // Generates a human-readable, unique reference like EE-20260508-AB12CD
        // The date helps booking specialists recognise when the booking was
        // made; the 6-char GUID slice keeps it unique without being huge
        private static string GenerateBookingReference()
        {
            return $"EE-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        private bool BookingExists(int id) => _context.Bookings.Any(e => e.BookingId == id);
    }
}
