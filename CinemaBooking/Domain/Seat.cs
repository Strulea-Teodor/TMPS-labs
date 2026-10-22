namespace CinemaBooking.Domain;

/// <summary>
/// A single seat in a cinema hall, identified by row and number (e.g. "C7").
///
/// Lab 1: this is now the abstract "product" of the Factory Method pattern.
/// Concrete seat types (StandardSeat, VipSeat, ...) decide their own
/// price multiplier, and HallLayout subclasses decide which type to create.
/// </summary>
public abstract class Seat
{
    public char Row { get; }
    public int Number { get; }
    public bool IsTaken { get; private set; }

    /// <summary>Human-readable seat category, e.g. "Standard" or "VIP".</summary>
    public abstract string Category { get; }

    /// <summary>Multiplier applied to the screening's base price for this seat.</summary>
    public abstract decimal PriceMultiplier { get; }

    protected Seat(char row, int number)
    {
        Row = row;
        Number = number;
    }

    public string Label => $"{Row}{Number}";

    public void Take()
    {
        if (IsTaken)
            throw new InvalidOperationException($"Seat {Label} is already taken.");
        IsTaken = true;
    }

    public void Release() => IsTaken = false;

    public override string ToString() => Label;
}
