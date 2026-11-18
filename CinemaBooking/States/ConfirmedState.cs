using CinemaBooking.Domain;

namespace CinemaBooking.States;

/// <summary>
/// STATE - concrete state: the booking is paid and its seats are reserved.
/// </summary>
public class ConfirmedState : IBookingState
{
    public bool AcceptsPayment => false;
    public string Name => "Confirmed";

    public void Confirm(Booking booking, decimal amountPaid) =>
        throw new InvalidOperationException("This booking is already confirmed and paid.");

    public decimal Cancel(Booking booking)
    {
        decimal refund = booking.TotalPrice;

        booking.RefundedAmount = refund;
        booking.TransitionTo(new CancelledState());

        // Free the seats last: the screening announces it to its observers,
        // and by then the booking must already be in its final state.
        booking.Screening.ReleaseSeats(booking.Seats);

        return refund;
    }
}
