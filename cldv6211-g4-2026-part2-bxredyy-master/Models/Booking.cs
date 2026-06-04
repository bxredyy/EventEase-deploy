using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    // POE Part 1A: Booking entity — the "join" table between Venue and Event.
    //              ERD relationship: Venue (1)──(*) Booking (*)──(1) Event.
    // POE Part 1B: Becomes the Bookings table in SQL LocalDB.
    public class Booking
    {
        public int BookingId { get; set; }

        // Foreign key to Venue. The [ForeignKey] attribute below on
        // the Venue navigation property links them together.
        [Required(ErrorMessage = "Please select a venue.")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Please select an event.")]
        [Display(Name = "Event")]
        public int EventId { get; set; }

        // POE brief: "start & end dates" must be stored.
        // [DataType(Date)] tells the view to render an HTML5 date picker.
        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        // POE brief: "unique booking IDs". We auto-generate this in the
        // BookingsController as EE-yyyyMMdd-XXXXXX so each booking is
        // identifiable to a booking specialist without exposing raw DB ids.
        [Required]
        [StringLength(50)]
        [Display(Name = "Booking Reference")]
        public string BookingReference { get; set; } = string.Empty;

        // Navigation properties — populated by EF when we use .Include(...).
        // They let Razor views show the venue/event NAME instead of just an id.
        [ForeignKey("VenueId")]
        public Venue? Venue { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }
    }
}
