using CinemaBooking.Domain;

namespace CinemaBooking.Notifications;

/// <summary>The kinds of change a screening announces.</summary>
public enum ScreeningChange
{
    Rescheduled,
    SeatsReleased,
}

/// <summary>What happened, plus a human-readable description.</summary>
public record ScreeningEvent(ScreeningChange Kind, string Details);

/// <summary>
/// OBSERVER - the "Observer" interface.
///
/// Anyone who wants to hear about changes to a screening implements this
/// and subscribes to the screening. The Screening (the "Subject") knows its
/// observers only through this interface, so it does not depend on e-mail,
/// waitlists or anything else that reacts to it.
/// </summary>
public interface IScreeningObserver
{
    void OnScreeningChanged(Screening screening, ScreeningEvent change);
}
