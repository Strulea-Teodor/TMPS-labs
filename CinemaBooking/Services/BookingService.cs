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
/// Lab 2: the service now charges an IBookingOrder instead of a bare Booking.
/// Whether the order is plain tickets or tickets wrapped in three extras makes
/// no difference here (Decorator), and neither does whether the payment method
/// is one of ours or an adapted third-party SDK (Adapter).
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

        // 3. Only after a successful payment: record the amount, reserve the seats and persist.
        booking.TotalPrice = total;
        foreach (var seat in booking.Seats)
            seat.Take();

        _repository.Save(booking);
        return booking;
    }
}
