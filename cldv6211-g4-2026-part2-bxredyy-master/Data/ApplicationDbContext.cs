using EventEase.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Data
{
    // POE Part 1B: DbContext for the Venue, Event and Booking tables.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        // POE Part 3A: EventType lookup table.
        public DbSet<EventType> EventTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // POE Part 2B: stop venues being deleted while bookings exist.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            // POE Part 2B: stop events being deleted while bookings exist.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // POE Part 3A: link Event to EventType.
            modelBuilder.Entity<Event>()
                .HasOne(e => e.EventType)
                .WithMany(et => et.Events)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // POE Part 3A: pre-populated event type categories.
            modelBuilder.Entity<EventType>().HasData(
                new EventType { EventTypeId = 1, Name = "Conference",            Description = "Professional industry conferences and summits" },
                new EventType { EventTypeId = 2, Name = "Wedding",               Description = "Wedding ceremonies and receptions" },
                new EventType { EventTypeId = 3, Name = "Gala Dinner",           Description = "Formal evening dinners and award ceremonies" },
                new EventType { EventTypeId = 4, Name = "Corporate Meeting",     Description = "Business meetings and team-building events" },
                new EventType { EventTypeId = 5, Name = "Birthday & Private",    Description = "Birthday parties and private celebrations" },
                new EventType { EventTypeId = 6, Name = "Exhibition",            Description = "Exhibitions, trade shows and product launches" }
            );

            // POE Part 1B: seed venues.
            modelBuilder.Entity<Venue>().HasData(
                new Venue { VenueId = 1, Name = "The Grand Hall",          Capacity = 500, Location = "123 Main Street, Johannesburg", ImageUrl = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800" },
                new Venue { VenueId = 2, Name = "Sunset Pavilion",         Capacity = 200, Location = "45 Beach Road, Cape Town",      ImageUrl = "https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=800" },
                new Venue { VenueId = 3, Name = "Tech Conference Centre",  Capacity = 300, Location = "7 Innovation Drive, Pretoria",   ImageUrl = "https://images.unsplash.com/photo-1587825140708-dfaf72ae4b04?w=800" }
            );

            // POE Part 1B: seed events.
            modelBuilder.Entity<Event>().HasData(
                new Event
                {
                    EventId = 1,
                    Name = "Annual Gala Dinner",
                    Description = "An exclusive black-tie gala dinner celebrating the year's achievements.",
                    StartDate = new DateTime(2026, 8, 15),
                    EndDate = new DateTime(2026, 8, 15),
                    EventTypeId = 3,
                    ImageUrl = "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?w=800"
                },
                new Event
                {
                    EventId = 2,
                    Name = "Tech Summit 2026",
                    Description = "A two-day conference bringing together technology leaders and innovators.",
                    StartDate = new DateTime(2026, 10, 21),
                    EndDate = new DateTime(2026, 10, 22),
                    EventTypeId = 1,
                    ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800"
                },
                new Event
                {
                    EventId = 3,
                    Name = "Wedding Celebration",
                    Description = "A beautiful wedding reception for up to 200 guests.",
                    StartDate = new DateTime(2026, 12, 5),
                    EndDate = new DateTime(2026, 12, 5),
                    EventTypeId = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1519741497674-611481863552?w=800"
                }
            );
        }
    }
}
