using CinemaBooking.Domain;

namespace CinemaBooking.Halls;

/// <summary>
/// FACTORY METHOD - concrete creator. Every seat in this hall is a StandardSeat.
/// </summary>
public class StandardHallLayout : HallLayout
{
    public StandardHallLayout(int rows, int seatsPerRow) : base(rows, seatsPerRow) { }

    public override string Name => "Standard hall";

    protected override Seat CreateSeat(char row, int number) => new StandardSeat(row, number);
}
