using CinemaBooking.Domain;

namespace CinemaBooking.States;

/// <summary>
/// STATE - concrete state: the booking is built but not paid yet.
/// No seats are reserved and no money has changed hands.
/// </summary>
public class PendingState : IBookingState
{
    public bool AcceptsPayment => true;
    public string Name => "Pending";

    public void Confirm(Booking booking, decimal amountPaid)
    {
        var taken = booking.Seats.Where(s => s.IsTaken).ToList();
        if (taken.Count > 0)
            throw new InvalidOperationException(
                $"Seat(s) no longer available: {string.Join(", ", taken.Select(s => s.Label))}.");

        foreach (var seat in booking.Seats)
            seat.Take();

        booking.TotalPrice = amountPaid;
        booking.TransitionTo(new ConfirmedState());
    }

    public decimal Cancel(Booking booking)
    {
        // Nothing was paid and nothing was reserved, so there is nothing to give back.
        booking.TransitionTo(new CancelledState());
        return 0m;
    }
}
