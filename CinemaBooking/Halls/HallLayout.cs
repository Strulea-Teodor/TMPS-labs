using CinemaBooking.Domain;

namespace CinemaBooking.Halls;

/// <summary>
/// FACTORY METHOD - the "Creator".
///
/// GenerateSeats() contains the algorithm shared by every hall: walk the
/// rows and numbers and produce one seat per position. It never calls
/// `new StandardSeat(...)` or `new VipSeat(...)` directly; instead it defers
/// to the abstract CreateSeat() factory method, which each concrete layout
/// overrides to decide what kind of seat goes where.
///
/// Adding a new hall type (IMAX, 4DX, couples' seats...) means adding one
/// subclass - nothing in this class or in Screening changes (OCP again).
/// </summary>
public abstract class HallLayout
{
    public abstract string Name { get; }
    public int Rows { get; }
    public int SeatsPerRow { get; }

    protected HallLayout(int rows, int seatsPerRow)
    {
        if (rows <= 0 || rows > 26)
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be between 1 and 26.");
        if (seatsPerRow <= 0)
            throw new ArgumentOutOfRangeException(nameof(seatsPerRow));

        Rows = rows;
        SeatsPerRow = seatsPerRow;
    }

    /// <summary>
    /// The factory method. Subclasses decide which concrete Seat to build
    /// for a given position.
    /// </summary>
    protected abstract Seat CreateSeat(char row, int number);

    /// <summary>
    /// Template logic that uses the factory method. Every call produces a
    /// fresh set of seats, so each Screening gets its own seat state.
    /// </summary>
    public IReadOnlyList<Seat> GenerateSeats()
    {
        var seats = new List<Seat>(Rows * SeatsPerRow);

        for (int r = 0; r < Rows; r++)
        {
            char rowLetter = (char)('A' + r);
            for (int n = 1; n <= SeatsPerRow; n++)
                seats.Add(CreateSeat(rowLetter, n));
        }

        return seats;
    }
}
