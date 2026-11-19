using CinemaBooking.Domain;
using CinemaBooking.Halls;

namespace CinemaBooking.Schedule;

/// <summary>
/// SINGLETON - the cinema's one and only screening schedule.
///
/// There is exactly one timetable for the cinema, and every part of the app
/// (the booking flow, an admin panel, reports) must see the same screenings
/// and the same seat state. A second instance would mean two customers could
/// book the same seat in two different copies of the hall.
///
/// Implementation notes:
///  - the constructor is private, so `new CinemaSchedule()` is impossible
///    outside this class;
///  - Lazy&lt;T&gt; gives thread-safe, on-first-use initialisation without
///    writing a double-checked lock by hand;
///  - the class is sealed so nobody can subclass it and sneak a second
///    instance in through a derived constructor.
/// </summary>
public sealed class CinemaSchedule
{
    private static readonly Lazy<CinemaSchedule> _instance =
        new(() => new CinemaSchedule());

    public static CinemaSchedule Instance => _instance.Value;

    private readonly List<Screening> _screenings = new();
    private int _nextId = 1;

    private CinemaSchedule() { }

    public IReadOnlyList<Screening> Screenings => _screenings;

    public Screening AddScreening(Movie movie, DateTime startTime, decimal basePrice, HallLayout hall)
    {
        var screening = new Screening(_nextId++, movie, startTime, basePrice, hall);
        _screenings.Add(screening);
        return screening;
    }

    public Screening? FindById(int id) => _screenings.FirstOrDefault(s => s.Id == id);

    public IEnumerable<Screening> ForMovie(string title) =>
        _screenings.Where(s => s.Movie.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

    /// <summary>Cinema-wide free seat count, shown in the demo's summary line.</summary>
    public int TotalFreeSeats => _screenings.Sum(s => s.FreeSeats.Count());
}
