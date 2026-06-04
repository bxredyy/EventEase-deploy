namespace EventEase.Models
{
    // POE Part 2C: ViewModel that powers the consolidated Booking Search page
    //              It carries the user's search term AND the joined results
    public class BookingSearchViewModel
    {
        public string? SearchTerm { get; set; }
        // "id", "event", "venue", "reference", or null/empty = search all fields
        public string? SearchField { get; set; }
        public IEnumerable<BookingDisplayViewModel> Bookings { get; set; } = new List<BookingDisplayViewModel>();
    }

    // POE Part 2C: A flattened "row" combining fields from Booking + Venue + Event
    //              This is the "consolidated view" the rubric asks for —
    //              the user sees the venue/event NAMES instead of raw IDs
    public class BookingDisplayViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;
        public int VenueCapacity { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? EventDescription { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
