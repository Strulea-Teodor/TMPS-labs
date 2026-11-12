namespace CinemaBooking.Orders;

/// <summary>
/// DECORATOR - concrete decorator: 3D glasses, one pair per booked seat.
/// Shows that a decorator can use data from the order it wraps.
/// </summary>
public class Glasses3DExtra : OrderExtra
{
    private const decimal PricePerPair = 20m;

    public Glasses3DExtra(IBookingOrder inner) : base(inner) { }

    protected override string Label => $"3D glasses x{Booking.Seats.Count}";
    protected override decimal ExtraPrice => PricePerPair * Booking.Seats.Count;
}
