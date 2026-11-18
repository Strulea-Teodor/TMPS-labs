using CinemaBooking.Domain;

namespace CinemaBooking.Notifications;

/// <summary>
/// OBSERVER - concrete observer: a customer waiting for a seat. Only cares
/// about seats becoming free; a change of time is of no interest yet.
/// </summary>
public class WaitlistNotifier : IScreeningObserver
{
    private readonly Customer _customer;

    public WaitlistNotifier(Customer customer)
    {
        _customer = customer;
    }

    public void OnScreeningChanged(Screening screening, ScreeningEvent change)
    {
        if (change.Kind != ScreeningChange.SeatsReleased)
            return;

        Console.WriteLine($"    [waitlist SMS to {_customer.Name}] \"{screening.Movie.Title}\": {change.Details}. Book now!");
    }
}
