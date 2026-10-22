using CinemaBooking.Discounts;
using CinemaBooking.Domain;

namespace CinemaBooking.Services;

/// <summary>
/// SRP: only responsible for working out what a booking costs.
/// OCP: it loops over whatever IDiscount implementations it is given,
/// so adding a new discount never requires editing this class.
/// </summary>
public class PriceCalculator
{
    private readonly IReadOnlyList<IDiscount> _discounts;

    public PriceCalculator(IEnumerable<IDiscount> discounts)
    {
        _discounts = discounts.ToList();
    }

    public decimal Calculate(Booking booking)
    {
        // Each seat type carries its own multiplier (VIP = 1.5x, Standard = 1x),
        // so a premium hall prices itself without PriceCalculator knowing the types.
        decimal price = booking.Seats.Sum(seat => booking.Screening.BasePrice * seat.PriceMultiplier);

        foreach (var discount in _discounts)
        {
            if (discount.IsApplicable(booking))
                price = discount.Apply(booking, price);
        }

        return Math.Round(price, 2);
    }

    /// <summary>Names of the discounts that apply to this booking, for display.</summary>
    public IEnumerable<string> ApplicableDiscounts(Booking booking) =>
        _discounts.Where(d => d.IsApplicable(booking)).Select(d => d.Name);
}
