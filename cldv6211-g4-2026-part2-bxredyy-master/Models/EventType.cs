using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    // POE Part 3A: New EventType lookup table.
    public class EventType
    {
        public int EventTypeId { get; set; }

        [Required(ErrorMessage = "Event type name is required.")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
        [Display(Name = "Event Type")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
