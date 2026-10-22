namespace CinemaBooking.Domain;

/// <summary>
/// A customer's reservation of one or more seats for a screening.
///
/// SRP: this class only holds booking data. It does NOT know how to
/// calculate its price, how to save itself, or how to print a ticket -
/// those jobs belong to PriceCalculator, IBookingRepository and TicketPrinter.
///
/// Lab 1: instances are assembled by BookingBuilder, which validates the
/// seats before this constructor is ever reached.
/// </summary>
public class Booking
{
    public Guid Id { get; } = Guid.NewGuid();
    public Customer Customer { get; }
    public Screening Screening { get; }
    public IReadOnlyList<Seat> Seats { get; }
    public string? Note { get; }
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>Final amount after discounts; set once the price is calculated.</summary>
    public decimal TotalPrice { get; set; }

    public Booking(Customer customer, Screening screening, IReadOnlyList<Seat> seats, string? note = null)
    {
        if (seats.Count == 0)
            throw new ArgumentException("A booking needs at least one seat.", nameof(seats));

        Customer = customer;
        Screening = screening;
        Seats = seats;
        Note = note;
    }
}
