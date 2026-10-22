using CinemaBooking.Builders;
using CinemaBooking.Discounts;
using CinemaBooking.Domain;
using CinemaBooking.Halls;
using CinemaBooking.Payments;
using CinemaBooking.Repositories;
using CinemaBooking.Schedule;
using CinemaBooking.Services;

namespace CinemaBooking;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Cinema Booking System - Lab 1 (Creational patterns) ===\n");

        // --- Wire up dependencies (composition root) -----------------------
        var discounts = new List<IDiscount> { new StudentDiscount(), new TuesdayDiscount() };
        var priceCalculator = new PriceCalculator(discounts);
        var repository = new InMemoryBookingRepository();
        var printer = new TicketPrinter();

        SeedSchedule();

        DemoSingleton();
        DemoFactoryMethod();
        DemoBuilder(priceCalculator, repository, printer);

        Console.WriteLine($"Total bookings stored: {repository.GetAll().Count}");
        Console.WriteLine($"Free seats across the cinema: {CinemaSchedule.Instance.TotalFreeSeats}");
    }

    // ----------------------------------------------------------------------
    // SINGLETON: there is one CinemaSchedule. Both references below are the
    // same object, so a screening added through one is visible through the other.
    // ----------------------------------------------------------------------
    private static void DemoSingleton()
    {
        Header("1. SINGLETON - CinemaSchedule");

        var a = CinemaSchedule.Instance;
        var b = CinemaSchedule.Instance;

        Console.WriteLine($"  Same instance?            {ReferenceEquals(a, b)}");
        Console.WriteLine($"  Screenings via 'b':       {b.Screenings.Count}");

        // Add through 'a', observe through 'b' - there is only one list underneath.
        a.AddScreening(new Movie("Late-night classic", 95, "Horror"),
                       NextWeekday(DayOfWeek.Friday).AddHours(23), 70m,
                       new StandardHallLayout(rows: 2, seatsPerRow: 4));

        Console.WriteLine($"  ...after a.AddScreening(): {b.Screenings.Count}");
        Console.WriteLine("  // new CinemaSchedule() does not compile - the constructor is private.\n");

        Console.WriteLine("  Schedule:");
        foreach (var s in a.Screenings)
            Console.WriteLine("    " + s);
        Console.WriteLine();
    }

    // ----------------------------------------------------------------------
    // FACTORY METHOD: Screening asks its HallLayout for seats. The layout
    // subclass decides which Seat subclass to create for each position.
    // ----------------------------------------------------------------------
    private static void DemoFactoryMethod()
    {
        Header("2. FACTORY METHOD - HallLayout.CreateSeat()");

        foreach (var screening in CinemaSchedule.Instance.Screenings)
        {
            var byCategory = screening.Seats
                .GroupBy(s => s.Category)
                .Select(g => $"{g.Count()} x {g.Key} (x{g.First().PriceMultiplier})");

            Console.WriteLine($"  Screening #{screening.Id} / {screening.Hall.Name}:");
            Console.WriteLine($"    {string.Join(", ", byCategory)}");
            Console.WriteLine($"    Layout: {RenderSeatMap(screening)}");
        }
        Console.WriteLine();
    }

    // ----------------------------------------------------------------------
    // BUILDER: a Booking is assembled fluently, validated in Build(), and
    // only then handed to BookingService.
    // ----------------------------------------------------------------------
    private static void DemoBuilder(PriceCalculator priceCalculator, IBookingRepository repository, TicketPrinter printer)
    {
        Header("3. BUILDER - BookingBuilder");

        var schedule = CinemaSchedule.Instance;
        var ana = new Customer("Ana Popescu", "ana@example.com", isStudent: true);
        var ion = new Customer("Ion Rusu", "ion@example.com");

        // Scenario A: student books two seats in the premium hall, one of them VIP, pays by card.
        Console.WriteLine("  Scenario A: Ana (student) -> Dune, seats B3 + C3 (VIP), card");
        var bookingA = new BookingBuilder()
            .ForCustomer(ana)
            .ForScreening(schedule.FindById(1)!)
            .AddSeat("B3")
            .AddSeat("c3")                       // labels are normalised to upper-case
            .WithNote("Birthday outing")
            .Build();

        Confirm(bookingA, new CardPayment("4111222233334444"), priceCalculator, repository, printer);

        // Scenario B: regular customer, Tuesday screening, PayPal. Same BookingService class, different IPaymentMethod injected (DIP).
        Console.WriteLine("  Scenario B: Ion -> Paddington on Tuesday, seat A1, PayPal");
        var bookingB = new BookingBuilder()
            .ForCustomer(ion)
            .ForScreening(schedule.FindById(2)!)
            .AddSeats("A1")
            .Build();

        Confirm(bookingB, new PayPalPayment("ion@example.com"), priceCalculator, repository, printer);

        // Scenario C: the builder refuses to build an invalid booking.
        Console.WriteLine("  Scenario C: builder validation");
        TryBuild("missing screening", () => new BookingBuilder().ForCustomer(ion).AddSeat("A1").Build());
        TryBuild("seat B3 already taken", () => new BookingBuilder().ForCustomer(ion).ForScreening(schedule.FindById(1)!).AddSeat("B3").Build());
        TryBuild("seat Z9 does not exist", () => new BookingBuilder().ForCustomer(ion).ForScreening(schedule.FindById(1)!).AddSeat("Z9").Build());
        TryBuild("same seat twice", () => new BookingBuilder().ForCustomer(ion).ForScreening(schedule.FindById(3)!).AddSeats("A1", "A1").Build());
        Console.WriteLine();
    }

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------
    private static void SeedSchedule()
    {
        var schedule = CinemaSchedule.Instance;

        var dune        = new Movie("Dune: Part Three", 166, "Sci-Fi");
        var paddington  = new Movie("Paddington in Peru", 106, "Family");
        var oppenheimer = new Movie("Oppenheimer", 180, "Drama");

        var nextTuesday  = NextWeekday(DayOfWeek.Tuesday);
        var nextSaturday = NextWeekday(DayOfWeek.Saturday);

        // Each screening is given a HallLayout; the layout's factory method decides the seat types.
        schedule.AddScreening(dune,        nextSaturday.AddHours(19), 120m, new PremiumHallLayout(rows: 3, seatsPerRow: 6, vipRows: 1));
        schedule.AddScreening(paddington,  nextTuesday.AddHours(17),   90m, new StandardHallLayout(rows: 2, seatsPerRow: 5));
        schedule.AddScreening(oppenheimer, nextSaturday.AddHours(21), 110m, new PremiumHallLayout(rows: 4, seatsPerRow: 6, vipRows: 2));
    }

    private static void Confirm(
        Booking booking,
        IPaymentMethod payment,
        PriceCalculator priceCalculator,
        IBookingRepository repository,
        TicketPrinter printer)
    {
        var service = new BookingService(priceCalculator, repository, payment);
        service.Confirm(booking);

        var applied = priceCalculator.ApplicableDiscounts(booking).ToList();
        Console.WriteLine("  Discounts applied: " + (applied.Count > 0 ? string.Join("; ", applied) : "none"));

        printer.Print(booking);
    }

    private static void TryBuild(string label, Func<Booking> build)
    {
        try
        {
            build();
            Console.WriteLine($"    {label,-28} -> built (unexpected!)");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"    {label,-28} -> rejected: {ex.Message}");
        }
    }

    private static string RenderSeatMap(Screening screening)
    {
        // One character per seat, row by row: '.' standard free, 'V' VIP free, '#' taken.
        var rows = screening.Seats.GroupBy(s => s.Row).OrderBy(g => g.Key);
        return string.Join(" | ", rows.Select(r =>
            $"{r.Key}: " + string.Concat(r.OrderBy(s => s.Number).Select(s =>
                s.IsTaken ? '#' : (s is VipSeat) ? 'V' : '.'))));
    }

    private static DateTime NextWeekday(DayOfWeek day)
    {
        var today = DateTime.Today;
        int daysAhead = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (daysAhead == 0) daysAhead = 7;
        return today.AddDays(daysAhead);
    }

    private static void Header(string title)
    {
        Console.WriteLine(new string('-', 60));
        Console.WriteLine(title);
        Console.WriteLine(new string('-', 60));
    }
}
