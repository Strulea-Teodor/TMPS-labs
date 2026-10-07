namespace CinemaBooking.Domain;

/// <summary>
/// A single seat in a cinema hall, identified by row and number (e.g. "C7").
/// </summary>
public class Seat
{
    public char Row { get; }
    public int Number { get; }
    public bool IsTaken { get; private set; }

    public Seat(char row, int number)
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
