namespace CinemaBooking.Domain;

/// <summary>
/// A film that the cinema can show. Holds only movie data.
/// </summary>
public class Movie
{
    public string Title { get; }
    public int DurationMinutes { get; }
    public string Genre { get; }

    public Movie(string title, int durationMinutes, string genre)
    {
        Title = title;
        DurationMinutes = durationMinutes;
        Genre = genre;
    }

    public override string ToString() => $"{Title} ({Genre}, {DurationMinutes} min)";
}
