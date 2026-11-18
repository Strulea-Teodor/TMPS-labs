using System.Globalization;
using CinemaBooking.Builders;
using CinemaBooking.Commands;
using CinemaBooking.Discounts;
using CinemaBooking.Domain;
using CinemaBooking.Facade;
using CinemaBooking.Halls;
using CinemaBooking.Notifications;
using CinemaBooking.Orders;
using CinemaBooking.Payments;
using CinemaBooking.Repositories;
using CinemaBooking.Schedule;
using CinemaBooking.Services;

namespace CinemaBooking;

public static class Program
{
    public static void Main()
    {
        // Same number and date formatting on every machine, whatever its regional settings.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Console.WriteLine("=== Cinema Booking System - Lab 3 (Behavioral patterns) ===\n");

        // --- Wire up dependencies (composition root) -----------------------
        var discounts = new List<IDiscount> { new StudentDiscount(), new TuesdayDiscount() };
        var priceCalculator = new PriceCalculator(discounts);
        var repository = new InMemoryBookingRepository();
        var printer = new TicketPrinter();
        var cinema = new CinemaFacade(priceCalculator, repository, printer);

        SeedSchedule();

        DemoState(priceCalculator, repository);
        DemoObserver(cinema);
        DemoCommand(cinema);

        Header("Summary");
        cinema.ShowSummary();
    }

    // ----------------------------------------------------------------------
    // STATE: the same two calls - Confirm() and Cancel() - do different
    // things depending on the booking's current state object.
    // ----------------------------------------------------------------------
    private static void DemoState(PriceCalculator priceCalculator, IBookingRepository repository)
    {
        Header("1. STATE - booking lifecycle");

        var screening = CinemaSchedule.Instance.FindById(3)!;
        var ana = new Customer("Ana Popescu", "ana@example.com", isStudent: true);
        var service = new BookingService(priceCalculator, repository, new CardPayment("4111222233334444"));

        Console.WriteLine("  Booking A: Ana -> Oppenheimer, seats A1 + A2");
        var booking = new BookingBuilder().ForCustomer(ana).ForScreening(screening).AddSeats("A1", "A2").Build();
        ShowState("after Build()", booking);

        service.Confirm(booking);                       // pays, then booking.Confirm(total)
        ShowState("after payment", booking);

        Try("Confirm() again", () => booking.Confirm(booking.TotalPrice));

        decimal refund = booking.Cancel();
        Console.WriteLine($"    Cancel() -> refund {refund:0.00} MDL");
        ShowState("after Cancel()", booking);

        Try("Cancel() again", () => booking.Cancel());
        Try("Confirm() after cancel", () => booking.Confirm(100m));

        Console.WriteLine("\n  Booking B: Ana -> Oppenheimer, seat B1, cancelled before paying");
        var unpaid = new BookingBuilder().ForCustomer(ana).ForScreening(screening).AddSeat("B1").Build();
        ShowState("after Build()", unpaid);
        Console.WriteLine($"    Cancel() -> refund {unpaid.Cancel():0.00} MDL (same call, different state, different result)");
        ShowState("after Cancel()", unpaid);
        Console.WriteLine();
    }

    // ----------------------------------------------------------------------
    // OBSERVER: the screening announces its changes; each subscriber decides
    // for itself whether and how to react.
    // ----------------------------------------------------------------------
    private static void DemoObserver(CinemaFacade cinema)
    {
        Header("2. OBSERVER - screening notifications");

        var screening = CinemaSchedule.Instance.FindById(2)!;
        var ion = new Customer("Ion Rusu", "ion@example.com");
        var maria = new Customer("Maria Ceban", "maria@example.com", isStudent: true);

        Console.WriteLine("  Ion books seat A1 for Paddington:");
        var ionsBooking = cinema.BookTickets(ion, 2, new[] { "A1" }, new PayPalPayment("ion@example.com"));

        var ionsNotifier = new BookedCustomerNotifier(ion);     // wants to know about time changes
        var mariasWaitlist = new WaitlistNotifier(maria);       // wants to know about free seats
        screening.Subscribe(ionsNotifier);
        screening.Subscribe(mariasWaitlist);
        Console.WriteLine($"  Subscribed: Ion (ticket holder), Maria (waitlist). Observers: {screening.ObserverCount}\n");

        DateTime original = screening.StartTime;

        Console.WriteLine("  The screening is moved two hours later:");
        screening.Reschedule(original.AddHours(2));

        Console.WriteLine("\n  Ion cancels his booking, so his seat is released:");
        decimal refund = ionsBooking.Cancel();
        Console.WriteLine($"    (Ion is refunded {refund:0.00} MDL)");

        screening.Unsubscribe(ionsNotifier);
        Console.WriteLine($"\n  Ion unsubscribes. Observers: {screening.ObserverCount}");
        Console.WriteLine("  The screening is moved back - nobody left who cares about the time:");
        screening.Reschedule(original);
        Console.WriteLine("    (no notifications sent)\n");

        screening.Unsubscribe(mariasWaitlist);
    }

    // ----------------------------------------------------------------------
    // COMMAND: every action is an object. The history runs them, logs them
    // and can undo them without knowing what they do.
    // ----------------------------------------------------------------------
    private static void DemoCommand(CinemaFacade cinema)
    {
        Header("3. COMMAND - actions with undo");

        var screening = CinemaSchedule.Instance.FindById(1)!;
        var ion = new Customer("Ion Rusu", "ion@example.com");
        var maria = new Customer("Maria Ceban", "maria@example.com", isStudent: true);
        var ana = new Customer("Ana Popescu", "ana@example.com", isStudent: true);
        var card = new CardPayment("4111222233334444");

        screening.Subscribe(new BookedCustomerNotifier(ion));   // Ion will hold a ticket for Dune
        screening.Subscribe(new WaitlistNotifier(ana));         // Ana waits for a seat at Dune

        var history = new CommandHistory();

        Console.WriteLine("  Executing three commands:");
        history.Execute(new BookTicketsCommand(cinema, ion, 1, new[] { "A1" }, card));
        history.Execute(new BookTicketsCommand(cinema, maria, 1, new[] { "B3", "C3" }, card, Extra.Popcorn));
        history.Execute(new RescheduleScreeningCommand(screening, screening.StartTime.AddHours(1)));

        Console.WriteLine("  A command that fails is not recorded:");
        try
        {
            history.Execute(new BookTicketsCommand(cinema, ana, 1, new[] { "A1" }, card));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"    Rejected: {ex.Message}");
        }

        Console.WriteLine($"\n  History ({history.Count} commands):");
        foreach (var entry in history.Log)
            Console.WriteLine($"    - {entry}");
        Console.WriteLine();
        cinema.ShowSeatMap(1);

        Console.WriteLine("  Undo #1 (the reschedule):");
        history.UndoLast();
        Console.WriteLine($"    Screening #1 now starts at {screening.StartTime:ddd dd MMM HH:mm}");

        Console.WriteLine("\n  Undo #2 (Maria's booking) - cancels it, refunds her, and the waitlist hears about it:");
        history.UndoLast();
        Console.WriteLine();
        cinema.ShowSeatMap(1);

        Console.WriteLine($"  History now ({history.Count} command): {string.Join("; ", history.Log)}\n");
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

    private static void ShowState(string moment, Booking booking)
    {
        string seats = string.Join(", ", booking.Seats.Select(s => $"{s.Label} {(s.IsTaken ? "taken" : "free")}"));
        Console.WriteLine($"    {moment,-16} state = {booking.State.Name,-10} paid = {booking.TotalPrice,7:0.00}   seats: {seats}");
    }

    private static void Try(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"    {label,-24} -> allowed (unexpected!)");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"    {label,-24} -> rejected: {ex.Message}");
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
