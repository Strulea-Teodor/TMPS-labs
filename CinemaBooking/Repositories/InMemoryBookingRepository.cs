using CinemaBooking.Domain;

namespace CinemaBooking.Repositories;

/// <summary>
/// SRP: this class is only responsible for storing bookings.
/// </summary>
public class InMemoryBookingRepository : IBookingRepository
{
    private readonly Dictionary<Guid, Booking> _bookings = new();

    public void Save(Booking booking) => _bookings[booking.Id] = booking;

    public Booking? GetById(Guid id) => _bookings.GetValueOrDefault(id);

    public IReadOnlyList<Booking> GetAll() => _bookings.Values.ToList();
}
