using CinemaBooking.Halls;

namespace CinemaBooking.Domain;

/// <summary>
/// A specific showing of a movie at a given time, with its own set of seats
/// and a base ticket price.
///
/// Lab 1: the seats are no longer built here. The Screening receives a
/// HallLayout and asks it to generate the seats (Factory Method), so this
/// class knows nothing about which concrete Seat types exist.
/// </summary>
public class Screening
{
    public int Id { get; }
    public Movie Movie { get; }
    public DateTime StartTime { get; }
    public decimal BasePrice { get; }
    public HallLayout Hall { get; }
    public IReadOnlyList<Seat> Seats { get; }

    public Screening(int id, Movie movie, DateTime startTime, decimal basePrice, HallLayout hall)
    {
        Id = id;
        Movie = movie;
        StartTime = startTime;
        BasePrice = basePrice;
        Hall = hall;
        Seats = hall.GenerateSeats();
    }

    public Seat? FindSeat(string label) =>
        Seats.FirstOrDefault(s => s.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<Seat> FreeSeats => Seats.Where(s => !s.IsTaken);

    public override string ToString() =>
        $"#{Id}  {Movie.Title,-22} {StartTime:ddd dd MMM HH:mm}   {BasePrice:0.00} MDL   {Hall.Name,-24} free: {FreeSeats.Count()}/{Seats.Count}";
}
