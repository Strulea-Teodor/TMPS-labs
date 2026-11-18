using CinemaBooking.Halls;
using CinemaBooking.Notifications;

namespace CinemaBooking.Domain;

/// <summary>
/// A specific showing of a movie at a given time, with its own set of seats
/// and a base ticket price. The seats come from the HallLayout it is given.
///
/// Lab 3 (OBSERVER - the "Subject"): a screening can change after tickets
/// were sold - it can be moved to another time, and seats can become free
/// again. Interested parties subscribe as IScreeningObserver and are told
/// about every change. The screening has no idea who they are or what they
/// do with the news.
/// </summary>
public class Screening
{
    private readonly List<IScreeningObserver> _observers = new();

    public int Id { get; }
    public Movie Movie { get; }
    public DateTime StartTime { get; private set; }
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

    // ---- Observer: subscription management --------------------------------

    public int ObserverCount => _observers.Count;

    public void Subscribe(IScreeningObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Unsubscribe(IScreeningObserver observer) => _observers.Remove(observer);

    // ---- Changes that observers are told about -----------------------------

    /// <summary>Moves the screening to another time and notifies observers.</summary>
    public void Reschedule(DateTime newStartTime)
    {
        if (newStartTime == StartTime)
            return;

        DateTime oldStartTime = StartTime;
        StartTime = newStartTime;

        Notify(new ScreeningEvent(
            ScreeningChange.Rescheduled,
            $"moved from {oldStartTime:ddd dd MMM HH:mm} to {newStartTime:ddd dd MMM HH:mm}"));
    }

    /// <summary>Frees the given seats (after a cancellation) and notifies observers.</summary>
    public void ReleaseSeats(IEnumerable<Seat> seats)
    {
        var released = seats.Where(s => s.IsTaken).ToList();
        if (released.Count == 0)
            return;

        foreach (var seat in released)
            seat.Release();

        Notify(new ScreeningEvent(
            ScreeningChange.SeatsReleased,
            $"seat{(released.Count == 1 ? "" : "s")} {string.Join(", ", released.Select(s => s.Label))} just became free"));
    }

    private void Notify(ScreeningEvent change)
    {
        // Iterate over a copy so an observer may unsubscribe while being notified.
        foreach (var observer in _observers.ToList())
            observer.OnScreeningChanged(this, change);
    }

    public override string ToString() =>
        $"#{Id}  {Movie.Title,-22} {StartTime:ddd dd MMM HH:mm}   {BasePrice:0.00} MDL   {Hall.Name,-26} free: {FreeSeats.Count()}/{Seats.Count}";
}
