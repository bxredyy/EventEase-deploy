using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Event Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Event start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Event end date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Event Image")]
        public string? ImageUrl { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
