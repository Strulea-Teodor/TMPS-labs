using CinemaBooking.Domain;

namespace CinemaBooking.Discounts;

/// <summary>A fixed amount off every ticket for screenings on Tuesdays.</summary>
public class TuesdayDiscount : IDiscount
{
    private const decimal AmountOffPerTicket = 15m;

    public string Name => $"Tuesday discount ({AmountOffPerTicket:0} MDL off per ticket)";

    public bool IsApplicable(Booking booking) =>
        booking.Screening.StartTime.DayOfWeek == DayOfWeek.Tuesday;

    public decimal Apply(Booking booking, decimal currentPrice) =>
        Math.Max(0, currentPrice - AmountOffPerTicket * booking.Seats.Count);
}
