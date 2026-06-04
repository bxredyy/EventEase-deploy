using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    // POE Part 1A: Event entity — second of the three required ERD tables.
    // POE Part 1B: Code-First model that EF turns into the Events table.
    public class Event
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Event Name")]
        public string Name { get; set; } = string.Empty;

        // Description is optional (no [Required]) and capped at 500 chars.
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }


        //Add StartDate and EndDate to Event Model
        [Required(ErrorMessage = "Event start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Event end date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }


        // POE Part 2A: Stored as an Azurite blob URL when uploaded.
        [Display(Name = "Event Image")]
        public string? ImageUrl { get; set; }

        // POE Part 2B: lets us check event.Bookings.Any() before allowing delete.
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
