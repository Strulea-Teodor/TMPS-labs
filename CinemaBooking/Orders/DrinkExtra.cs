namespace CinemaBooking.Orders;

/// <summary>DECORATOR - concrete decorator: one soft drink for the order.</summary>
public class DrinkExtra : OrderExtra
{
    public DrinkExtra(IBookingOrder inner) : base(inner) { }

    protected override string Label => "Soft drink";
    protected override decimal ExtraPrice => 35m;
}
