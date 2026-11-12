using CinemaBooking.Builders;
using CinemaBooking.Discounts;
using CinemaBooking.Domain;
using CinemaBooking.Facade;
using CinemaBooking.Halls;
using CinemaBooking.Orders;
using CinemaBooking.Payments;
using CinemaBooking.Payments.External;
using CinemaBooking.Repositories;
using CinemaBooking.Schedule;
using CinemaBooking.Services;

namespace CinemaBooking;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Cinema Booking System - Lab 2 (Structural patterns) ===\n");

        // --- Wire up dependencies (composition root) -----------------------
        var discounts = new List<IDiscount> { new StudentDiscount(), new TuesdayDiscount() };
        var priceCalculator = new PriceCalculator(discounts);
        var repository = new InMemoryBookingRepository();
        var printer = new TicketPrinter();

        SeedSchedule();

        DemoDecorator(priceCalculator);
        DemoAdapter(priceCalculator, repository, printer);
        DemoFacade(priceCalculator, repository, printer);
    }

    // ----------------------------------------------------------------------
    // DECORATOR: extras are wrapped around a tickets order one layer at a
    // time. Every layer is still an IBookingOrder, so Total and Lines keep
    // working no matter how many layers there are.
    // ----------------------------------------------------------------------
    private static void DemoDecorator(PriceCalculator priceCalculator)
    {
        Header("1. DECORATOR - order extras");

        var ana = new Customer("Ana Popescu", "ana@example.com", isStudent: true);
        var booking = new BookingBuilder()
            .ForCustomer(ana)
            .ForScreening(CinemaSchedule.Instance.FindById(1)!)
            .AddSeats("A1", "A2")
            .Build();

        IBookingOrder order = new TicketsOrder(booking, priceCalculator.Calculate(booking));
        ShowOrder("tickets only", order);

        order = new PopcornExtra(order);
        ShowOrder("+ popcorn", order);

        order = new DrinkExtra(order);
        ShowOrder("+ drink", order);

        order = new DrinkExtra(order);              // the same decorator can be applied twice
        ShowOrder("+ second drink", order);

        order = new Glasses3DExtra(order);          // priced per seat, read from the wrapped order
        ShowOrder("+ 3D glasses", order);

        Console.WriteLine("\n  Receipt lines of the fully wrapped order:");
        foreach (var line in order.Lines)
            Console.WriteLine($"    {line.Description,-18}{line.Amount,8:0.00} MDL");
        Console.WriteLine("  (nothing was paid or reserved here - this only shows the wrapping)\n");
    }

    // ----------------------------------------------------------------------
    // ADAPTER: BookingService expects an IPaymentMethod. The QuickPay SDK has
    // a completely different API. QuickPayAdapter makes one look like the other.
    // ----------------------------------------------------------------------
    private static void DemoAdapter(PriceCalculator priceCalculator, IBookingRepository repository, TicketPrinter printer)
    {
        Header("2. ADAPTER - third-party QuickPay SDK as an IPaymentMethod");

        var schedule = CinemaSchedule.Instance;
        var ion = new Customer("Ion Rusu", "ion@example.com");
        var quickPay = new QuickPayClient(merchantId: "CINEMA42");      // the incompatible third-party object

        // The SDK on its own - note the different method, units and return type.
        var raw = quickPay.SubmitCharge(amountMinorUnits: 100, currencyIso: "MDL", reference: "connection test");
        Console.WriteLine($"  Raw SDK call:   SubmitCharge(100, \"MDL\", ...) -> {raw.StatusCode} {raw.Message}");
        Console.WriteLine("  Our interface:  bool Pay(decimal amount)\n");

        // Scenario A: adapted payment succeeds. BookingService is unchanged and unaware.
        Console.WriteLine("  Scenario A: Ion -> Paddington on Tuesday, seat B2, QuickPay");
        IPaymentMethod payment = new QuickPayAdapter(quickPay, reference: "Ion Rusu / screening 2");
        var service = new BookingService(priceCalculator, repository, payment);

        var booking = new BookingBuilder().ForCustomer(ion).ForScreening(schedule.FindById(2)!).AddSeat("B2").Build();
        service.Confirm(booking);
        printer.Print(new TicketsOrder(booking, booking.TotalPrice));

        // Scenario B: the SDK declines (over its 500 MDL limit). The adapter turns the
        // status code into `false`, and the service reacts exactly as for any other failure.
        Console.WriteLine("  Scenario B: Ion -> Oppenheimer, 4 VIP seats (660 MDL), QuickPay limit is 500 MDL");
        var bigBooking = new BookingBuilder()
            .ForCustomer(ion)
            .ForScreening(schedule.FindById(3)!)
            .AddSeats("D1", "D2", "D3", "D4")
            .Build();
        try
        {
            service.Confirm(bigBooking);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  Rejected: {ex.Message}");
            Console.WriteLine($"  Seat D1 still free? {!schedule.FindById(3)!.FindSeat("D1")!.IsTaken}\n");
        }
    }

    // ----------------------------------------------------------------------
    // FACADE: the same purchase flow as above, but the client makes ONE call.
    // It hands the facade its dependencies once and never calls the builder,
    // calculator, decorators, service or printer itself.
    // ----------------------------------------------------------------------
    private static void DemoFacade(PriceCalculator priceCalculator, IBookingRepository repository, TicketPrinter printer)
    {
        Header("3. FACADE - CinemaFacade");

        var cinema = new CinemaFacade(priceCalculator, repository, printer);
        var maria = new Customer("Maria Ceban", "maria@example.com", isStudent: true);

        cinema.ShowProgramme();

        Console.WriteLine("  One call: Maria (student) -> Dune, B3 + C3 (VIP), card, popcorn + drink + 3D glasses");
        cinema.BookTickets(
            maria,
            1,                                      // screening id
            new[] { "B3", "C3" },                   // seats
            new CardPayment("4111222233334444"),    // any IPaymentMethod
            Extra.Popcorn, Extra.Drink, Extra.Glasses3D);

        cinema.ShowSeatMap(1);

        Console.WriteLine("  The facade passes subsystem errors through unchanged:");
        TryBook("screening #99", () => cinema.BookTickets(maria, 99, new[] { "A1" }, new CardPayment("4111222233334444")));
        TryBook("seat B3 again", () => cinema.BookTickets(maria, 1, new[] { "B3" }, new CardPayment("4111222233334444")));
        Console.WriteLine();

        cinema.ShowSummary();
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

        schedule.AddScreening(dune,        nextSaturday.AddHours(19), 120m, new PremiumHallLayout(rows: 3, seatsPerRow: 6, vipRows: 1));
        schedule.AddScreening(paddington,  nextTuesday.AddHours(17),   90m, new StandardHallLayout(rows: 2, seatsPerRow: 5));
        schedule.AddScreening(oppenheimer, nextSaturday.AddHours(21), 110m, new PremiumHallLayout(rows: 4, seatsPerRow: 6, vipRows: 2));
    }

    private static void ShowOrder(string label, IBookingOrder order) =>
        Console.WriteLine($"  {label,-16} total = {order.Total,7:0.00} MDL   ({order.Lines.Count} line{(order.Lines.Count == 1 ? "" : "s")})");

    private static void TryBook(string label, Action book)
    {
        try
        {
            book();
            Console.WriteLine($"    {label,-16} -> booked (unexpected!)");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"    {label,-16} -> rejected: {ex.Message}");
        }
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
