namespace CinemaBooking.Domain;

/// <summary>
/// The person making a booking.
/// </summary>
public class Customer
{
    public string Name { get; }
    public string Email { get; }
    public bool IsStudent { get; }

    public Customer(string name, string email, bool isStudent = false)
    {
        Name = name;
        Email = email;
        IsStudent = isStudent;
    }

    public override string ToString() => $"{Name} <{Email}>";
}
