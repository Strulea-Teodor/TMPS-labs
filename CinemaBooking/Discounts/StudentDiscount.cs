using CinemaBooking.Domain;

namespace CinemaBooking.Discounts;

/// <summary>20% off the whole booking for students.</summary>
public class StudentDiscount : IDiscount
{
    public string Name => "Student discount (20%)";

    public bool IsApplicable(Booking booking) => booking.Customer.IsStudent;

    public decimal Apply(Booking booking, decimal currentPrice) => currentPrice * 0.80m;
}
