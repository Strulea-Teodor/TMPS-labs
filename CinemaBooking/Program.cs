using CinemaBooking.Discounts;
using CinemaBooking.Domain;
using CinemaBooking.Payments;
using CinemaBooking.Repositories;
using CinemaBooking.Services;

namespace CinemaBooking;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Cinema Booking System - Lab 0 (SOLID) ===\n");

        // --- Seed data -----------------------------------------------------
        var screenings = CreateScreenings();

        // --- Wire up dependencies (composition root) -----------------------
        // OCP: to add a new discount, add one more line here - nothing else changes.
        var discounts = new List<IDiscount>
        {
            new StudentDiscount(),
            new TuesdayDiscount(),
        };

        var priceCalculator = new PriceCalculator(discounts);
        var repository = new InMemoryBookingRepository();
        var printer = new TicketPrinter();

        // --- Scenario 1: a student pays by card ----------------------------
        Console.WriteLine("Available screenings:");
        foreach (var s in screenings)
            Console.WriteLine("  " + s);
        Console.WriteLine();

        var ana = new Customer("Ana Popescu", "ana@example.com", isStudent: true);
        RunBooking(
            new BookingService(priceCalculator, repository, new CardPayment("4111222233334444")),
            priceCalculator, printer,
            ana, screenings[0], new[] { "B3", "B4" });

        // --- Scenario 2: a regular customer pays with PayPal on a Tuesday ---
        // DIP in action: same BookingService, different IPaymentMethod.
        var ion = new Customer("Ion Rusu", "ion@example.com");
        RunBooking(
            new BookingService(priceCalculator, repository, new PayPalPayment("ion@example.com")),
            priceCalculator, printer,
            ion, screenings[1], new[] { "A1" });

        // --- Scenario 3: trying to take a seat that is already booked -------
        Console.WriteLine("Scenario 3: double-booking seat B3...");
        try
        {
            var service = new BookingService(priceCalculator, repository, new CardPayment("5555666677778888"));
            service.Book(ion, screenings[0], new[] { "B3" });
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  Rejected: {ex.Message}\n");
        }

        // --- Summary -------------------------------------------------------
        Console.WriteLine($"Total bookings stored: {repository.GetAll().Count}");
        Console.WriteLine("Seats left in screening #1: " +
                          string.Join(" ", screenings[0].FreeSeats.Select(s => s.Label)));
    }

    private static void RunBooking(
        BookingService service,
        PriceCalculator priceCalculator,
        TicketPrinter printer,
        Customer customer,
        Screening screening,
        string[] seats)
    {
        Console.WriteLine($"Booking for {customer} -> screening #{screening.Id}, seats {string.Join(", ", seats)}");

        var booking = service.Book(customer, screening, seats);

        var applied = priceCalculator.ApplicableDiscounts(booking).ToList();
        Console.WriteLine("  Discounts applied: " + (applied.Count > 0 ? string.Join("; ", applied) : "none"));

        printer.Print(booking);
    }

    private static List<Screening> CreateScreenings()
    {
        var dune = new Movie("Dune: Part Three", 166, "Sci-Fi");
        var paddington = new Movie("Paddington in Peru", 106, "Family");
        var oppenheimer = new Movie("Oppenheimer", 180, "Drama");

        // Pick the next Tuesday so the TuesdayDiscount demo always fires.
        var today = DateTime.Today;
        int daysUntilTuesday = ((int)DayOfWeek.Tuesday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilTuesday == 0) daysUntilTuesday = 7;
        var nextTuesday = today.AddDays(daysUntilTuesday);

        // And a Saturday for the non-discounted case.
        int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0) daysUntilSaturday = 7;
        var nextSaturday = today.AddDays(daysUntilSaturday);

        return new List<Screening>
        {
            new Screening(1, dune,        nextSaturday.AddHours(19), basePrice: 120m, rows: 3, seatsPerRow: 6),
            new Screening(2, paddington,  nextTuesday.AddHours(17),  basePrice: 90m,  rows: 2, seatsPerRow: 5),
            new Screening(3, oppenheimer, nextSaturday.AddHours(21), basePrice: 110m, rows: 3, seatsPerRow: 6),
        };
    }
}
