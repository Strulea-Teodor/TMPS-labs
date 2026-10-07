namespace CinemaBooking.Payments;

public class CardPayment : IPaymentMethod
{
    private readonly string _maskedCardNumber;

    public CardPayment(string cardNumber)
    {
        _maskedCardNumber = "**** **** **** " + cardNumber[^4..];
    }

    public string Name => $"Card {_maskedCardNumber}";

    public bool Pay(decimal amount)
    {
        Console.WriteLine($"  [Card] Charging {amount:0.00} MDL to {_maskedCardNumber}... approved.");
        return true;
    }
}
