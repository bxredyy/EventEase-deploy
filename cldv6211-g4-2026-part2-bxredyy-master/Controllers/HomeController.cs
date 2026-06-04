using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    // The default landing page. Shows a quick dashboard with the count of
    // venues, events, and bookings so the booking specialist can see at a
    // glance how busy the system is.
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor injection — ASP.NET Core's DI container hands us the
        // DbContext. We don't have to manually new() one up
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            var model = new DashboardViewModel
            {
                VenueCount = await _context.Venues.CountAsync(),
                EventCount = await _context.Events.CountAsync(),
                BookingCount = await _context.Bookings.CountAsync()
            };
            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
