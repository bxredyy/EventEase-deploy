using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventEase.Models
{
    // POE Part 3A: filters for the advanced search page.
    public class BookingSearchViewModel
    {
        public string? SearchTerm { get; set; }
        public string? SearchField { get; set; }

        [Display(Name = "Event Type")]
        public int? EventTypeId { get; set; }

        [Display(Name = "Date From")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "Date To")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        [Display(Name = "Venue")]
        public int? VenueId { get; set; }

        [Display(Name = "Show only available venues")]
        public bool ShowAvailableOnly { get; set; }

        public IEnumerable<SelectListItem> EventTypeOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> VenueOptions     { get; set; } = new List<SelectListItem>();

        public IEnumerable<BookingDisplayViewModel> Bookings { get; set; } = new List<BookingDisplayViewModel>();

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchTerm) ||
            EventTypeId.HasValue ||
            DateFrom.HasValue ||
            DateTo.HasValue ||
            VenueId.HasValue ||
            ShowAvailableOnly;
    }

    public class BookingDisplayViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;
        public int VenueCapacity { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? EventDescription { get; set; }
        public string? EventTypeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
