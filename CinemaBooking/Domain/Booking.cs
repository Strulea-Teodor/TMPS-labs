using CinemaBooking.States;

namespace CinemaBooking.Domain;

/// <summary>
/// A customer's reservation of one or more seats for a screening.
///
/// SRP: this class holds the booking data. It does NOT know how to
/// calculate its price, how to save itself, or how to print a ticket -
/// those jobs belong to PriceCalculator, IBookingRepository and TicketPrinter.
///
/// Lab 3 (STATE - the "Context"): the booking has a lifecycle. Confirm() and
/// Cancel() are forwarded to the current IBookingState object, which does the
/// work for that stage and switches the booking to the next state. Booking
/// itself contains no `if (state == ...)` logic.
/// </summary>
public class Booking
{
    public Guid Id { get; } = Guid.NewGuid();
    public Customer Customer { get; }
    public Screening Screening { get; }
    public IReadOnlyList<Seat> Seats { get; }
    public string? Note { get; }
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>Amount actually charged: tickets after discounts plus any extras. Set once paid.</summary>
    public decimal TotalPrice { get; internal set; }

    /// <summary>Amount given back when a paid booking was cancelled.</summary>
    public decimal RefundedAmount { get; internal set; }

    /// <summary>Where the booking is in its lifecycle. Every booking starts as Pending.</summary>
    public IBookingState State { get; private set; } = new PendingState();

    public Booking(Customer customer, Screening screening, IReadOnlyList<Seat> seats, string? note = null)
    {
        if (seats.Count == 0)
            throw new ArgumentException("A booking needs at least one seat.", nameof(seats));

        Customer = customer;
        Screening = screening;
        Seats = seats;
        Note = note;
    }

    /// <summary>Marks the booking as paid. What happens depends on the current state.</summary>
    public void Confirm(decimal amountPaid) => State.Confirm(this, amountPaid);

    /// <summary>Cancels the booking and returns the refund. What happens depends on the current state.</summary>
    public decimal Cancel() => State.Cancel(this);

    /// <summary>Called by the state objects to move the booking to its next state.</summary>
    internal void TransitionTo(IBookingState next) => State = next;
}
