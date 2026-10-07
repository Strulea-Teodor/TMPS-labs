using CinemaBooking.Domain;

namespace CinemaBooking.Discounts;

/// <summary>
/// OCP: new discount rules are added by writing a new class that implements
/// this interface. PriceCalculator never has to change.
/// </summary>
public interface IDiscount
{
    string Name { get; }

    /// <summary>Returns true if this discount applies to the given booking.</summary>
    bool IsApplicable(Booking booking);

    /// <summary>Returns the new price for the booking after applying the discount.</summary>
    decimal Apply(Booking booking, decimal currentPrice);
}
