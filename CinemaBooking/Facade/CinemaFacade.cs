using CinemaBooking.Builders;
using CinemaBooking.Domain;
using CinemaBooking.Orders;
using CinemaBooking.Payments;
using CinemaBooking.Repositories;
using CinemaBooking.Schedule;
using CinemaBooking.Services;

namespace CinemaBooking.Facade;

/// <summary>
/// FACADE - one simple entry point to the whole booking subsystem.
///
/// Buying a ticket involves seven collaborators: CinemaSchedule (find the
/// screening), BookingBuilder (assemble and validate), PriceCalculator
/// (discounts), the order decorators (extras), BookingService (pay, reserve),
/// IBookingRepository (store) and TicketPrinter (print). Client code such as
/// Program.cs - or later a web controller or a GUI - should not have to know
/// that sequence.
///
/// The facade knows the sequence and exposes it as a single call,
/// BookTickets(...). It adds no business rules of its own; it only
/// coordinates. The subsystem classes stay public, so anything unusual can
/// still be done by using them directly.
/// </summary>
public class CinemaFacade
{
    private readonly CinemaSchedule _schedule = CinemaSchedule.Instance;
    private readonly PriceCalculator _priceCalculator;
    private readonly IBookingRepository _repository;
    private readonly TicketPrinter _printer;

    public CinemaFacade(PriceCalculator priceCalculator, IBookingRepository repository, TicketPrinter printer)
    {
        _priceCalculator = priceCalculator;
        _repository = repository;
        _printer = printer;
    }

    /// <summary>Prints every screening on the schedule.</summary>
    public void ShowProgramme()
    {
        Console.WriteLine("  Programme:");
        foreach (var screening in _schedule.Screenings)
            Console.WriteLine("    " + screening);
        Console.WriteLine();
    }

    /// <summary>Prints the seat map of one screening: '.' free, 'V' free VIP, '#' taken.</summary>
    public void ShowSeatMap(int screeningId)
    {
        var screening = FindScreening(screeningId);

        Console.WriteLine($"  Seats for #{screening.Id} {screening.Movie.Title} ({screening.Hall.Name}):");
        foreach (var row in screening.Seats.GroupBy(s => s.Row).OrderBy(g => g.Key))
        {
            var cells = row.OrderBy(s => s.Number)
                           .Select(s => s.IsTaken ? '#' : (s is VipSeat) ? 'V' : '.');
            Console.WriteLine($"    {row.Key}  {string.Join(' ', cells)}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// The whole purchase in one call: find the screening, build the booking,
    /// price it, add extras, take payment, reserve the seats, save, print.
    /// Throws if the screening or a seat is invalid, or if the payment fails.
    /// </summary>
    public Booking BookTickets(
        Customer customer,
        int screeningId,
        IEnumerable<string> seatLabels,
        IPaymentMethod payment,
        params Extra[] extras)
    {
        // 1. Schedule (Singleton): which screening?
        var screening = FindScreening(screeningId);

        // 2. Builder: assemble and validate the booking.
        var booking = new BookingBuilder()
            .ForCustomer(customer)
            .ForScreening(screening)
            .AddSeats(seatLabels.ToArray())
            .Build();

        // 3. Price the tickets (discounts), then wrap the order in extras (Decorator).
        IBookingOrder order = new TicketsOrder(booking, _priceCalculator.Calculate(booking));
        foreach (var extra in extras)
            order = ExtraCatalog.Add(order, extra);

        // 4. Pay, reserve, save. The payment method may be an Adapter - the service cannot tell.
        var service = new BookingService(_priceCalculator, _repository, payment);
        service.Confirm(order);

        // 5. Report.
        var discounts = _priceCalculator.ApplicableDiscounts(booking).ToList();
        Console.WriteLine("  Discounts applied: " + (discounts.Count > 0 ? string.Join("; ", discounts) : "none"));
        _printer.Print(order);

        return booking;
    }

    /// <summary>Prints how many bookings exist and how many seats are still free.</summary>
    public void ShowSummary()
    {
        Console.WriteLine($"  Bookings stored:              {_repository.GetAll().Count}");
        Console.WriteLine($"  Revenue:                      {_repository.GetAll().Sum(b => b.TotalPrice):0.00} MDL");
        Console.WriteLine($"  Free seats across the cinema: {_schedule.TotalFreeSeats}");
    }

    private Screening FindScreening(int screeningId) =>
        _schedule.FindById(screeningId)
        ?? throw new ArgumentException($"There is no screening #{screeningId}.");
}
