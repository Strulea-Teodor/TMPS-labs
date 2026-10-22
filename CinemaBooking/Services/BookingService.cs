using CinemaBooking.Domain;
using CinemaBooking.Payments;
using CinemaBooking.Repositories;

namespace CinemaBooking.Services;

/// <summary>
/// Orchestrates the booking flow: calculate price -> pay -> reserve seats -> save.
///
/// DIP: this high-level class depends only on abstractions
/// (IPaymentMethod, IBookingRepository). The concrete implementations are
/// injected through the constructor, so they can be swapped freely.
///
/// Lab 1: the "assemble and validate" step moved out into BookingBuilder,
/// so this service receives a fully formed Booking and only has to process it.
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

    public Booking Confirm(Booking booking)
    {
        // Guard against a race: the builder checked availability at build time,
        // but another booking could have taken a seat since then.
        var alreadyTaken = booking.Seats.Where(s => s.IsTaken).ToList();
        if (alreadyTaken.Count > 0)
            throw new InvalidOperationException(
                $"Seat(s) no longer available: {string.Join(", ", alreadyTaken.Select(s => s.Label))}.");

        // 1. Price it.
        booking.TotalPrice = _priceCalculator.Calculate(booking);

        // 2. Take payment through whatever method was injected.
        Console.WriteLine($"  Paying with {_paymentMethod.Name}:");
        if (!_paymentMethod.Pay(booking.TotalPrice))
            throw new InvalidOperationException("Payment failed. Seats were not reserved.");

        // 3. Only after a successful payment: reserve the seats and persist.
        foreach (var seat in booking.Seats)
            seat.Take();

        _repository.Save(booking);
        return booking;
    }
}
