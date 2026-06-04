using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class BookingFormViewModel
    {
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Please select a venue.")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Please select an event.")]
        [Display(Name = "Event")]
        public int EventId { get; set; }

        public string BookingReference { get; set; } = string.Empty;

        // POE Part 2B: booking dates are taken from the selected event.
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }

        public IEnumerable<SelectListItem> Venues { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Events { get; set; } = new List<SelectListItem>();
    }
}
