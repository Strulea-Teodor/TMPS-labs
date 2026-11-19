using CinemaBooking.Payments.External;

namespace CinemaBooking.Payments;

/// <summary>
/// ADAPTER - makes the third-party QuickPayClient usable wherever the app
/// expects an IPaymentMethod.
///
/// BookingService only knows `bool Pay(decimal amount)`. QuickPay only knows
/// `SubmitCharge(long minorUnits, string currency, string reference)`. This
/// class sits between them and translates in both directions:
///   - decimal MDL        -> whole bani
///   - (nothing)          -> currency code + merchant reference
///   - receipt.StatusCode -> true / false
///
/// Neither BookingService nor the SDK had to change (object adapter: the
/// adaptee is held as a field, not inherited from).
/// </summary>
public class QuickPayAdapter : IPaymentMethod
{
    private const string Currency = "MDL";
    private const int Approved = 200;

    private readonly QuickPayClient _client;
    private readonly string _reference;

    public QuickPayAdapter(QuickPayClient client, string reference)
    {
        _client = client;
        _reference = reference;
    }

    public string Name => "QuickPay (online)";

    public bool Pay(decimal amount)
    {
        long minorUnits = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

        QuickPayReceipt receipt = _client.SubmitCharge(minorUnits, Currency, _reference);

        Console.WriteLine($"  [QuickPay] {minorUnits} bani -> {receipt.StatusCode} {receipt.Message} [{receipt.TransactionId}]");
        return receipt.StatusCode == Approved;
    }
}
