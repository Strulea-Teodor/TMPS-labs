using CinemaBooking.Domain;

namespace CinemaBooking.States;

/// <summary>
/// STATE - the "State" interface.
///
/// A booking goes through a lifecycle: Pending -> Confirmed -> Cancelled
/// (or Pending -> Cancelled). What "confirm" and "cancel" mean depends on
/// where in that lifecycle the booking is. Instead of `if (status == ...)`
/// checks inside Booking, each stage is its own class that implements these
/// operations for that stage and decides which state comes next.
/// </summary>
public interface IBookingState
{
    string Name { get; }

    /// <summary>The payment went through: reserve the seats and record the amount.</summary>
    void Confirm(Booking booking, decimal amountPaid);

    /// <summary>Cancels the booking and returns the amount to refund.</summary>
    decimal Cancel(Booking booking);
}
