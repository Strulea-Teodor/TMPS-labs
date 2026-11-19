namespace CinemaBooking.Domain;

/// <summary>
/// A specific showing of a movie at a given time, with its own set of seats
/// and a base ticket price.
/// </summary>
public class Screening
{
    public int Id { get; }
    public Movie Movie { get; }
    public DateTime StartTime { get; }
    public decimal BasePrice { get; }
    public IReadOnlyList<Seat> Seats { get; }

    public Screening(int id, Movie movie, DateTime startTime, decimal basePrice, int rows, int seatsPerRow)
    {
        Id = id;
        Movie = movie;
        StartTime = startTime;
        BasePrice = basePrice;

        var seats = new List<Seat>();
        for (int r = 0; r < rows; r++)
        {
            char rowLetter = (char)('A' + r);
            for (int n = 1; n <= seatsPerRow; n++)
                seats.Add(new Seat(rowLetter, n));
        }
        Seats = seats;
    }

    public Seat? FindSeat(string label) =>
        Seats.FirstOrDefault(s => s.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<Seat> FreeSeats => Seats.Where(s => !s.IsTaken);

    public override string ToString() =>
        $"#{Id}  {Movie.Title,-22} {StartTime:ddd dd MMM HH:mm}   {BasePrice:0.00} MDL   free seats: {FreeSeats.Count()}/{Seats.Count}";
}
