using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    // POE Part 1A: Venue entity — one of the three required tables in the ERD
    //              (Venue, Event, Booking).
    // POE Part 1B: Used by Entity Framework Code-First to generate the Venues table
    public class Venue
    {
        // Primary key. EF auto-detects "VenueId" by naming convention
        // and configures it as IDENTITY (auto-increment).
        public int VenueId { get; set; }

        // They tell EF the column is NOT NULL with nvarchar(100),
        // AND tell ASP.NET to validate user input on the form
        [Required(ErrorMessage = "Venue name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Venue Name")]
        public string Name { get; set; } = string.Empty;

        // [Range] adds a minimum/maximum check, capacity can never be 0 or negative
        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100,000.")]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        // POE Part 1B: placeholder URL in Part 1.
        // POE Part 2A: replaced with an Azurite blob URL once an image is uploaded
        // The "?" makes it nullable, a venue can exist without an image
        [Display(Name = "Venue Image")]
        public string? ImageUrl { get; set; }

        // Navigation property, gives us venue.Bookings in code
        // POE Part 2B: used to check whether a venue has bookings before deleting
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
