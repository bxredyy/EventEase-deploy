using EventEase.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Data
{
    // POE Part 1B: The EF Core "DbContext" — the bridge between our C# models
    //              and the SQL LocalDB database. Every CRUD operation in the
    //              controllers goes through this class.
    // POE Part 1C: EF reads the connection string from appsettings.json (set up
    //              in Program.cs) so the DbContext talks to the LocalDB instance.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Each DbSet becomes a table in the database.
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        // OnModelCreating uses the "Fluent API" — extra rules that go beyond
        // what the [Required]/[StringLength] attributes can express.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // POE Part 2B: ON DELETE RESTRICT — the database itself REFUSES to
            //              delete a Venue if any Booking still references it.
            //              This is a second line of defence on top of the
            //              controller check (venue.Bookings.Any()).
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            // POE Part 2B: Same restrict rule for Events — protects the
            //              database from orphaned booking rows.
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // POE Part 1B: Seed data — three Venues and three Events that EF
            //              inserts when the migration runs. Means the marker
            //              sees a populated database the first time they run.
            //              ImageUrl uses placeholder URLs (Unsplash) as required
            //              by Part 1's "use placeholder URLs" instruction.
            modelBuilder.Entity<Venue>().HasData(
                new Venue
                {
                    VenueId = 1,
                    Name = "The Grand Hall",
                    Capacity = 500,
                    Location = "123 Main Street, Johannesburg",
                    ImageUrl = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800"
                },
                new Venue
                {
                    VenueId = 2,
                    Name = "Sunset Pavilion",
                    Capacity = 200,
                    Location = "45 Beach Road, Cape Town",
                    ImageUrl = "https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=800"
                },
                new Venue
                {
                    VenueId = 3,
                    Name = "Tech Conference Centre",
                    Capacity = 300,
                    Location = "7 Innovation Drive, Pretoria",
                    ImageUrl = "https://images.unsplash.com/photo-1587825140708-dfaf72ae4b04?w=800"
                }
            );

            modelBuilder.Entity<Event>().HasData(
                new Event
                {
                    EventId = 1,
                    Name = "Annual Gala Dinner",
                    Description = "An exclusive black-tie gala dinner celebrating the year's achievements.",
                    ImageUrl = "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?w=800"
                },
                new Event
                {
                    EventId = 2,
                    Name = "Tech Summit 2026",
                    Description = "A two-day conference bringing together technology leaders and innovators.",
                    ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800"
                },
                new Event
                {
                    EventId = 3,
                    Name = "Wedding Celebration",
                    Description = "A beautiful wedding reception for up to 200 guests.",
                    ImageUrl = "https://images.unsplash.com/photo-1519741497674-611481863552?w=800"
                }
            );
        }
    }
}
