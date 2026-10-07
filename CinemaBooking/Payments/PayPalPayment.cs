namespace CinemaBooking.Payments;

public class PayPalPayment : IPaymentMethod
{
    private readonly string _email;

    public PayPalPayment(string email)
    {
        _email = email;
    }

    public string Name => $"PayPal ({_email})";

    public bool Pay(decimal amount)
    {
        Console.WriteLine($"  [PayPal] Requesting {amount:0.00} MDL from {_email}... completed.");
        return true;
    }
}
