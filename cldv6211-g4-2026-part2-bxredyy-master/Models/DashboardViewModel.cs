namespace EventEase.Models
{
    // ViewModel for the home page dashboard
    // ViewModels are the modern best practice — they give compile-time
    // checking and IntelliSense in the .cshtml view
    public class DashboardViewModel
    {
        public int VenueCount { get; set; }
        public int EventCount { get; set; }
        public int BookingCount { get; set; }
    }
}
