using CinemaBooking.Domain;

namespace CinemaBooking.Builders;

/// <summary>
/// BUILDER - assembles a Booking step by step through a fluent interface.
///
/// A booking has several parts that arrive at different moments in a real UI:
/// the customer logs in, then picks a screening, then clicks seats one by one,
/// maybe adds a note. Collecting all of that in a single constructor call
/// would be awkward, and a half-built Booking object floating around would be
/// a bug waiting to happen.
///
/// The builder keeps the partial state, lets seats be added incrementally,
/// and performs all validation in one place when Build() is called. Only a
/// fully valid Booking ever leaves this class.
/// </summary>
public class BookingBuilder
{
    private Customer? _customer;
    private Screening? _screening;
    private readonly List<string> _seatLabels = new();
    private string? _note;

    public BookingBuilder ForCustomer(Customer customer)
    {
        _customer = customer;
        return this;
    }

    public BookingBuilder ForScreening(Screening screening)
    {
        _screening = screening;
        // Seats belong to a screening; changing the screening invalidates them.
        _seatLabels.Clear();
        return this;
    }

    public BookingBuilder AddSeat(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Seat label cannot be empty.", nameof(label));

        _seatLabels.Add(label.Trim().ToUpperInvariant());
        return this;
    }

    public BookingBuilder AddSeats(params string[] labels)
    {
        foreach (var label in labels)
            AddSeat(label);
        return this;
    }

    public BookingBuilder WithNote(string note)
    {
        _note = note;
        return this;
    }

    /// <summary>
    /// Validates everything collected so far and produces the Booking.
    /// Throws if a required part is missing or a seat is invalid.
    /// </summary>
    public Booking Build()
    {
        if (_customer is null)
            throw new InvalidOperationException("A booking needs a customer. Call ForCustomer() first.");
        if (_screening is null)
            throw new InvalidOperationException("A booking needs a screening. Call ForScreening() first.");
        if (_seatLabels.Count == 0)
            throw new InvalidOperationException("A booking needs at least one seat. Call AddSeat().");

        var duplicates = _seatLabels.GroupBy(l => l).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException($"Seat(s) added more than once: {string.Join(", ", duplicates)}.");

        var seats = new List<Seat>(_seatLabels.Count);
        foreach (var label in _seatLabels)
        {
            var seat = _screening.FindSeat(label)
                ?? throw new ArgumentException($"Seat '{label}' does not exist in {_screening.Hall.Name}.");

            if (seat.IsTaken)
                throw new InvalidOperationException($"Seat {seat.Label} is already taken.");

            seats.Add(seat);
        }

        return new Booking(_customer, _screening, seats, _note);
    }

    /// <summary>Clears all state so the same builder can be reused for the next booking.</summary>
    public BookingBuilder Reset()
    {
        _customer = null;
        _screening = null;
        _seatLabels.Clear();
        _note = null;
        return this;
    }
}
