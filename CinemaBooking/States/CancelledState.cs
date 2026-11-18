using CinemaBooking.Domain;

namespace CinemaBooking.States;

/// <summary>
/// STATE - concrete state: the end of the lifecycle. Nothing can be done
/// with a cancelled booking any more.
/// </summary>
public class CancelledState : IBookingState
{
    public bool AcceptsPayment => false;
    public string Name => "Cancelled";

    public void Confirm(Booking booking, decimal amountPaid) =>
        throw new InvalidOperationException("A cancelled booking cannot be confirmed.");

    public decimal Cancel(Booking booking) =>
        throw new InvalidOperationException("This booking is already cancelled.");
}
