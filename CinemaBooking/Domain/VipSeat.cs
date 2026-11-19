namespace CinemaBooking.Domain;

/// <summary>A wider, reclining seat in the back rows, sold at a premium.</summary>
public class VipSeat : Seat
{
    public VipSeat(char row, int number) : base(row, number) { }

    public override string Category => "VIP";
    public override decimal PriceMultiplier => 1.5m;

    public override string ToString() => $"{Label}(VIP)";
}
