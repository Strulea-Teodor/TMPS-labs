namespace CinemaBooking.Domain;

/// <summary>A regular seat sold at the screening's base price.</summary>
public class StandardSeat : Seat
{
    public StandardSeat(char row, int number) : base(row, number) { }

    public override string Category => "Standard";
    public override decimal PriceMultiplier => 1.0m;
}
