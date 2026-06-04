using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    // POE Part 1B/2B/2C/3A: Booking CRUD, double-booking prevention, advanced search.
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .ToListAsync();
            return View(bookings);
        }

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

        public async Task<IActionResult> Create()
        {
            var viewModel = new BookingFormViewModel
            {
                Venues = await GetVenueSelectList(),
                Events = await GetEventSelectList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // POE Part 2B: booking dates come from the selected event.
                var selectedEvent = await _context.Events.FindAsync(viewModel.EventId);
                if (selectedEvent == null)
                {
                    ModelState.AddModelError("EventId", "Selected event not found.");
                    viewModel.Venues = await GetVenueSelectList(viewModel.VenueId);
                    viewModel.Events = await GetEventSelectList(viewModel.EventId);
                    return View(viewModel);
                }

                // POE Part 2B: prevent double-booking using event dates.
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

                // POE Part 2B: prevent double-booking, ignoring the booking being edited.
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

        // POE Part 3A: advanced search with event type, date range, venue and availability filters.
        public async Task<IActionResult> Search(
            string? searchTerm,
            string? searchField,
            int? eventTypeId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? venueId,
            bool showAvailableOnly = false)
        {
            var query = _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event!).ThenInclude(e => e.EventType)
                .AsQueryable();

            // Text search by chosen field.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = searchField switch
                {
                    "id"        => query.Where(b => b.BookingId.ToString().Contains(term)),
                    "event"     => query.Where(b => b.Event!.Name.ToLower().Contains(term)),
                    "venue"     => query.Where(b => b.Venue!.Name.ToLower().Contains(term)),
                    "reference" => query.Where(b => b.BookingReference.ToLower().Contains(term)),
                    _ => query.Where(b =>
                            b.BookingId.ToString().Contains(term) ||
                            b.BookingReference.ToLower().Contains(term) ||
                            b.Event!.Name.ToLower().Contains(term) ||
                            b.Venue!.Name.ToLower().Contains(term))
                };
            }

            // Event type filter.
            if (eventTypeId.HasValue)
            {
                query = query.Where(b => b.Event!.EventTypeId == eventTypeId.Value);
            }

            // Date range filter.
            if (dateFrom.HasValue)
            {
                query = query.Where(b => b.EndDate >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                query = query.Where(b => b.StartDate <= dateTo.Value);
            }

            // Venue filter.
            if (venueId.HasValue)
            {
                query = query.Where(b => b.VenueId == venueId.Value);
            }

            var results = await query
                .OrderBy(b => b.StartDate)
                .ToListAsync();

            // Availability filter: list venues that are free in the chosen date range.
            if (showAvailableOnly && dateFrom.HasValue && dateTo.HasValue)
            {
                var occupiedVenueIds = await _context.Bookings
                    .Where(b => b.StartDate <= dateTo.Value && b.EndDate >= dateFrom.Value)
                    .Select(b => b.VenueId)
                    .Distinct()
                    .ToListAsync();

                var availableVenues = await _context.Venues
                    .Where(v => !occupiedVenueIds.Contains(v.VenueId))
                    .OrderBy(v => v.Name)
                    .ToListAsync();

                ViewBag.AvailableVenues = availableVenues;
                ViewBag.AvailabilityDateFrom = dateFrom.Value;
                ViewBag.AvailabilityDateTo = dateTo.Value;
            }

            var viewModel = new BookingSearchViewModel
            {
                SearchTerm = searchTerm,
                SearchField = searchField,
                EventTypeId = eventTypeId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                VenueId = venueId,
                ShowAvailableOnly = showAvailableOnly,
                EventTypeOptions = await GetEventTypeSelectList(eventTypeId),
                VenueOptions = await GetVenueOptionsList(venueId),
                Bookings = results.Select(b => new BookingDisplayViewModel
                {
                    BookingId = b.BookingId,
                    BookingReference = b.BookingReference,
                    VenueName = b.Venue?.Name ?? "Unknown",
                    VenueLocation = b.Venue?.Location ?? "Unknown",
                    VenueCapacity = b.Venue?.Capacity ?? 0,
                    EventName = b.Event?.Name ?? "Unknown",
                    EventDescription = b.Event?.Description,
                    EventTypeName = b.Event?.EventType?.Name,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate
                }).ToList()
            };

            return View(viewModel);
        }

        // Dropdown helper for the advanced search filters.
        private async Task<IEnumerable<SelectListItem>> GetEventTypeSelectList(int? selectedId)
        {
            return (await _context.EventTypes.OrderBy(t => t.Name).ToListAsync())
                .Select(t => new SelectListItem
                {
                    Value = t.EventTypeId.ToString(),
                    Text = t.Name,
                    Selected = selectedId.HasValue && t.EventTypeId == selectedId.Value
                });
        }

        private async Task<IEnumerable<SelectListItem>> GetVenueOptionsList(int? selectedId)
        {
            return (await _context.Venues.OrderBy(v => v.Name).ToListAsync())
                .Select(v => new SelectListItem
                {
                    Value = v.VenueId.ToString(),
                    Text = $"{v.Name} (Capacity: {v.Capacity:N0})",
                    Selected = selectedId.HasValue && v.VenueId == selectedId.Value
                });
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
