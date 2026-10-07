using CinemaBooking.Domain;
using CinemaBooking.Payments;
using CinemaBooking.Repositories;

namespace CinemaBooking.Services;

/// <summary>
/// Orchestrates the booking flow: reserve seats -> calculate price -> pay -> save.
///
/// DIP: this high-level class depends only on abstractions
/// (IPaymentMethod, IBookingRepository). The concrete implementations are
/// injected through the constructor, so they can be swapped freely.
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

    public Booking Book(Customer customer, Screening screening, IEnumerable<string> seatLabels)
    {
        // 1. Resolve and validate the requested seats.
        var seats = new List<Seat>();
        foreach (var label in seatLabels)
        {
            var seat = screening.FindSeat(label)
                ?? throw new ArgumentException($"Seat '{label}' does not exist in this hall.");

            if (seat.IsTaken)
                throw new InvalidOperationException($"Seat {seat.Label} is already taken.");

            seats.Add(seat);
        }

        // 2. Build the booking and price it.
        var booking = new Booking(customer, screening, seats);
        booking.TotalPrice = _priceCalculator.Calculate(booking);

        // 3. Take payment through whatever method was injected.
        Console.WriteLine($"  Paying with {_paymentMethod.Name}:");
        if (!_paymentMethod.Pay(booking.TotalPrice))
            throw new InvalidOperationException("Payment failed. Seats were not reserved.");

        // 4. Only after a successful payment: reserve the seats and persist.
        foreach (var seat in seats)
            seat.Take();

        _repository.Save(booking);
        return booking;
    }
}
