using CinemaBooking.Domain;

namespace CinemaBooking.Repositories;

/// <summary>
/// DIP: the service layer talks to this interface. The in-memory
/// implementation can later be swapped for a file or database one
/// without touching BookingService.
/// </summary>
public interface IBookingRepository
{
    void Save(Booking booking);
    Booking? GetById(Guid id);
    IReadOnlyList<Booking> GetAll();
}
