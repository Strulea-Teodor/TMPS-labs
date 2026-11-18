using CinemaBooking.Domain;
using CinemaBooking.Facade;
using CinemaBooking.Orders;
using CinemaBooking.Payments;

namespace CinemaBooking.Commands;

/// <summary>
/// COMMAND - concrete command: buy tickets. The "receiver" is CinemaFacade.
/// Undo cancels the booking that Execute created, which refunds the customer
/// and frees the seats.
/// </summary>
public class BookTicketsCommand : ICommand
{
    private readonly CinemaFacade _cinema;
    private readonly Customer _customer;
    private readonly int _screeningId;
    private readonly string[] _seatLabels;
    private readonly IPaymentMethod _payment;
    private readonly Extra[] _extras;

    private Booking? _booking;   // remembered by Execute so that Undo knows what to reverse

    public BookTicketsCommand(
        CinemaFacade cinema,
        Customer customer,
        int screeningId,
        string[] seatLabels,
        IPaymentMethod payment,
        params Extra[] extras)
    {
        _cinema = cinema;
        _customer = customer;
        _screeningId = screeningId;
        _seatLabels = seatLabels;
        _payment = payment;
        _extras = extras;
    }

    public string Description =>
        $"Book {string.Join(", ", _seatLabels)} on screening #{_screeningId} for {_customer.Name}";

    public void Execute()
    {
        _booking = _cinema.BookTickets(_customer, _screeningId, _seatLabels, _payment, _extras);
    }

    public void Undo()
    {
        if (_booking is null)
            throw new InvalidOperationException("Nothing to undo: this command was never executed.");

        decimal refund = _booking.Cancel();
        Console.WriteLine($"    Booking {_booking.Id.ToString()[..8].ToUpper()} cancelled, {refund:0.00} MDL refunded to {_customer.Name}.");
        _booking = null;
    }
}
