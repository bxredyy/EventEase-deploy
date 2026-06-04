using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Bookings
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

        // GET: /Bookings/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new BookingFormViewModel
            {
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
                // Fetch the selected event to get its scheduled dates.
                // Booking dates are derived from the event — not entered manually by the user.
                var selectedEvent = await _context.Events.FindAsync(viewModel.EventId);
                if (selectedEvent == null)
                {
                    ModelState.AddModelError("EventId", "Selected event not found.");
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                // Double-booking check: prevent the same venue from being booked
                // during an overlapping event period. Uses the EVENT's dates, not
                // a separate user-entered date range.
                bool hasConflict = await _context.Bookings.AnyAsync(b =>
                    b.VenueId == viewModel.VenueId &&
                    b.Event!.StartDate < selectedEvent.EndDate &&
                    b.Event!.EndDate > selectedEvent.StartDate);

                if (hasConflict)
                {
                    TempData["Error"] = $"This venue is already booked for an event that overlaps with '{selectedEvent.Name}' " +
                                        $"({selectedEvent.StartDate:dd MMM yyyy} – {selectedEvent.EndDate:dd MMM yyyy}). " +
                                        "Please choose a different venue or event.";
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                var booking = new Booking
                {
                    VenueId = viewModel.VenueId,
                    EventId = viewModel.EventId,
                    StartDate = selectedEvent.StartDate,
                    EndDate = selectedEvent.EndDate,
                    BookingReference = GenerateBookingReference()
                };

                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Booking created successfully! Reference: {booking.BookingReference}";
                return RedirectToAction(nameof(Index));
            }

            viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
            viewModel.Events = await GetEventSelectList(viewModel.EventId);
            return View(viewModel);
        }

        // GET: /Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return NotFound();

            var viewModel = new BookingFormViewModel
            {
                BookingId = booking.BookingId,
                VenueId = booking.VenueId,
                EventId = booking.EventId,
                BookingReference = booking.BookingReference,
                EventStartDate = booking.Event?.StartDate,
                EventEndDate = booking.Event?.EndDate,
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
                var selectedEvent = await _context.Events.FindAsync(viewModel.EventId);
                if (selectedEvent == null)
                {
                    ModelState.AddModelError("EventId", "Selected event not found.");
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                // Double-booking check — exclude the current booking from conflict search.
                bool hasConflict = await _context.Bookings.AnyAsync(b =>
                    b.VenueId == viewModel.VenueId &&
                    b.BookingId != viewModel.BookingId &&
                    b.Event!.StartDate < selectedEvent.EndDate &&
                    b.Event!.EndDate > selectedEvent.StartDate);

                if (hasConflict)
                {
                    TempData["Error"] = $"This venue is already booked for an event that overlaps with '{selectedEvent.Name}' " +
                                        $"({selectedEvent.StartDate:dd MMM yyyy} – {selectedEvent.EndDate:dd MMM yyyy}). " +
                                        "Please choose a different venue or event.";
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
                    booking.StartDate = selectedEvent.StartDate;
                    booking.EndDate = selectedEvent.EndDate;

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

        // GET: /Bookings/Search
        public async Task<IActionResult> Search(string? searchTerm, string? searchField)
        {
            var query = _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();

                query = searchField switch
                {
                    "id" => query.Where(b => b.BookingId.ToString().Contains(term)),
                    "event" => query.Where(b => b.Event!.Name.ToLower().Contains(term)),
                    "venue" => query.Where(b => b.Venue!.Name.ToLower().Contains(term)),
                    "reference" => query.Where(b => b.BookingReference.ToLower().Contains(term)),
                    _ => query.Where(b =>
                            b.BookingId.ToString().Contains(term) ||
                            b.BookingReference.ToLower().Contains(term) ||
                            b.Event!.Name.ToLower().Contains(term) ||
                            b.Venue!.Name.ToLower().Contains(term))
                };
            }

            var results = await query.ToListAsync();

            var viewModel = new BookingSearchViewModel
            {
                SearchTerm = searchTerm,
                SearchField = searchField,
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
                    Text = $"{e.Name} ({e.StartDate:dd MMM yyyy} – {e.EndDate:dd MMM yyyy})",
                    Selected = e.EventId == selectedId
                });
        }

        private static string GenerateBookingReference()
        {
            return $"EE-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        private bool BookingExists(int id) => _context.Bookings.Any(e => e.BookingId == id);
    }
}
