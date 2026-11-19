using CinemaBooking.Domain;

namespace CinemaBooking.Halls;

/// <summary>
/// FACTORY METHOD - concrete creator. The last N rows are VIP seats,
/// everything in front of them is standard.
/// </summary>
public class PremiumHallLayout : HallLayout
{
    private readonly int _vipRows;

    public PremiumHallLayout(int rows, int seatsPerRow, int vipRows) : base(rows, seatsPerRow)
    {
        if (vipRows < 0 || vipRows > rows)
            throw new ArgumentOutOfRangeException(nameof(vipRows), "VIP rows must fit inside the hall.");
        _vipRows = vipRows;
    }

    public override string Name => $"Premium hall ({_vipRows} VIP row{(_vipRows == 1 ? "" : "s")})";

    protected override Seat CreateSeat(char row, int number)
    {
        int rowIndex = row - 'A';                 // A -> 0, B -> 1, ...
        bool isVipRow = rowIndex >= Rows - _vipRows;

        return isVipRow
            ? new VipSeat(row, number)
            : new StandardSeat(row, number);
    }
}
