namespace CinemaBooking.Payments;

/// <summary>
/// DIP: BookingService depends on this abstraction, not on a concrete
/// payment class, so any payment method can be plugged in.
/// </summary>
public interface IPaymentMethod
{
    string Name { get; }

    /// <summary>Attempts to charge the given amount. Returns true on success.</summary>
    bool Pay(decimal amount);
}
