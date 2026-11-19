using CinemaBooking.Domain;

namespace CinemaBooking.Orders;

/// <summary>
/// DECORATOR - the "Concrete Component": just the tickets, no extras.
/// This is the object that decorators wrap.
/// </summary>
public class TicketsOrder : IBookingOrder
{
    private readonly decimal _ticketsPrice;

    /// <param name="booking">The booking the tickets belong to.</param>
    /// <param name="ticketsPrice">Price of the seats after discounts (from PriceCalculator).</param>
    public TicketsOrder(Booking booking, decimal ticketsPrice)
    {
        if (ticketsPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(ticketsPrice), "Price cannot be negative.");

        Booking = booking;
        _ticketsPrice = ticketsPrice;
    }

    public Booking Booking { get; }

    public IReadOnlyList<OrderLine> Lines => new[]
    {
        new OrderLine($"Tickets x{Booking.Seats.Count}", _ticketsPrice)
    };

    public decimal Total => _ticketsPrice;
}
