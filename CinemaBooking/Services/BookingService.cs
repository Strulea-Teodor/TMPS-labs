using CinemaBooking.Domain;
using CinemaBooking.Orders;
using CinemaBooking.Payments;
using CinemaBooking.Repositories;

namespace CinemaBooking.Services;

/// <summary>
/// Processes an order: pay -> reserve seats -> save.
///
/// DIP: payment, storage and the order itself are known only through
/// abstractions (IPaymentMethod, IBookingRepository, IBookingOrder), so
/// their concrete implementations can be swapped freely.
///
/// The service charges an IBookingOrder: plain tickets or tickets wrapped in
/// extras, it makes no difference here. What "confirmed" means for the booking
/// is decided by the booking's current state object (State pattern).
/// </summary>
public class BookingService
{
    private readonly PriceCalculator _priceCalculator;
    private readonly IBookingRepository _repository;
    private readonly IPaymentMethod _paymentMethod;

    public BookingService(
        PriceCalculator priceCalculator,
        IBookingRepository repository,
        IPaymentMethod paymentMethod)
    {
        _priceCalculator = priceCalculator;
        _repository = repository;
        _paymentMethod = paymentMethod;
    }

    /// <summary>Confirms a booking with no extras: tickets only.</summary>
    public Booking Confirm(Booking booking) =>
        Confirm(new TicketsOrder(booking, _priceCalculator.Calculate(booking)));

    public Booking Confirm(IBookingOrder order)
    {
        var booking = order.Booking;

        // Guard against a race: the builder checked availability at build time,
        // but another booking could have taken a seat since then.
        var alreadyTaken = booking.Seats.Where(s => s.IsTaken).ToList();
        if (alreadyTaken.Count > 0)
            throw new InvalidOperationException(
                $"Seat(s) no longer available: {string.Join(", ", alreadyTaken.Select(s => s.Label))}.");

        // 1. The order already knows its total (tickets + any extras).
        decimal total = order.Total;

        // 2. Take payment through whatever method was injected.
        Console.WriteLine($"  Paying with {_paymentMethod.Name}:");
        if (!_paymentMethod.Pay(total))
            throw new InvalidOperationException("Payment failed. Seats were not reserved.");

        // 3. Only after a successful payment: the booking moves to its next state
        //    (Pending -> Confirmed), which reserves the seats and records the amount.
        booking.Confirm(total);

        _repository.Save(booking);
        return booking;
    }
}
