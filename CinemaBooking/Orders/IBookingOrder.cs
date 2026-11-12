using CinemaBooking.Domain;

namespace CinemaBooking.Orders;

/// <summary>One line on the receipt: what was bought and what it cost.</summary>
public record OrderLine(string Description, decimal Amount);

/// <summary>
/// DECORATOR - the "Component".
///
/// An order is what the customer actually pays for: the tickets of a booking,
/// optionally wrapped in any number of extras (popcorn, drink, 3D glasses).
/// Both the plain tickets and every extra implement this same interface, so
/// the rest of the app (BookingService, TicketPrinter) never needs to know
/// how many layers of extras an order has.
/// </summary>
public interface IBookingOrder
{
    Booking Booking { get; }

    /// <summary>Everything on the receipt, in the order it was added.</summary>
    IReadOnlyList<OrderLine> Lines { get; }

    /// <summary>The amount to charge for the whole order.</summary>
    decimal Total { get; }
}
