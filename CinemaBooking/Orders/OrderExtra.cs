using CinemaBooking.Domain;

namespace CinemaBooking.Orders;

/// <summary>
/// DECORATOR - the base "Decorator".
///
/// It IS an IBookingOrder and it HAS an IBookingOrder. Everything is forwarded
/// to the wrapped order, and the extra adds its own line and its own price on
/// top. Because the wrapped object is only known by its interface, extras can
/// be stacked in any order and any number:
///
///     new DrinkExtra(new PopcornExtra(new TicketsOrder(booking, price)))
///
/// Concrete extras only say what they are called and what they cost.
/// </summary>
public abstract class OrderExtra : IBookingOrder
{
    private readonly IBookingOrder _inner;

    protected OrderExtra(IBookingOrder inner)
    {
        _inner = inner;
    }

    protected abstract string Label { get; }
    protected abstract decimal ExtraPrice { get; }

    public Booking Booking => _inner.Booking;

    public IReadOnlyList<OrderLine> Lines =>
        _inner.Lines.Append(new OrderLine(Label, ExtraPrice)).ToList();

    public decimal Total => _inner.Total + ExtraPrice;
}
