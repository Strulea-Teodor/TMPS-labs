using CinemaBooking.Domain;
using CinemaBooking.Orders;

namespace CinemaBooking.Services;

/// <summary>
/// SRP: only responsible for formatting and printing a ticket.
/// </summary>
public class TicketPrinter
{
    /// <summary>
    /// Prints a ticket with an itemised receipt. Works for any IBookingOrder,
    /// however many extras are wrapped around the tickets.
    /// </summary>
    public void Print(IBookingOrder order)
    {
        var booking = order.Booking;

        const int width = 44;
        const string totalLabel = "TOTAL";
        string line = new string('=', width);

        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine(Center("CINEMA TICKET", width));
        Console.WriteLine(line);
        Console.WriteLine($"  Movie:     {booking.Screening.Movie.Title}");
        Console.WriteLine($"  Time:      {booking.Screening.StartTime:ddd, dd MMM yyyy HH:mm}");
        Console.WriteLine($"  Hall:      {booking.Screening.Hall.Name}");
        Console.WriteLine($"  Seats:     {string.Join(", ", booking.Seats.Select(s => $"{s.Label} ({s.Category})"))}");
        Console.WriteLine($"  Customer:  {booking.Customer.Name}");
        if (!string.IsNullOrWhiteSpace(booking.Note))
            Console.WriteLine($"  Note:      {booking.Note}");
        Console.WriteLine(new string('-', width));
        foreach (var item in order.Lines)
            Console.WriteLine($"  {item.Description,-26}{item.Amount,10:0.00} MDL");
        Console.WriteLine($"  {totalLabel,-26}{order.Total,10:0.00} MDL");
        Console.WriteLine(new string('-', width));
        Console.WriteLine($"  Booking #: {booking.Id.ToString()[..8].ToUpper()}");
        Console.WriteLine(line);
        Console.WriteLine();
    }

    private static string Center(string text, int width)
    {
        int padding = Math.Max(0, (width - text.Length) / 2);
        return new string(' ', padding) + text;
    }
}
