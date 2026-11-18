using CinemaBooking.Domain;

namespace CinemaBooking.Notifications;

/// <summary>
/// OBSERVER - concrete observer: a customer who holds a ticket and must be
/// told when the screening moves to another time. Ignores other events.
/// </summary>
public class BookedCustomerNotifier : IScreeningObserver
{
    private readonly Customer _customer;

    public BookedCustomerNotifier(Customer customer)
    {
        _customer = customer;
    }

    public void OnScreeningChanged(Screening screening, ScreeningEvent change)
    {
        if (change.Kind != ScreeningChange.Rescheduled)
            return;

        Console.WriteLine($"    [e-mail to {_customer.Email}] Your screening of \"{screening.Movie.Title}\" was {change.Details}.");
    }
}
