namespace CinemaBooking.Orders;

/// <summary>DECORATOR - concrete decorator: one large popcorn for the order.</summary>
public class PopcornExtra : OrderExtra
{
    public PopcornExtra(IBookingOrder inner) : base(inner) { }

    protected override string Label => "Large popcorn";
    protected override decimal ExtraPrice => 60m;
}
