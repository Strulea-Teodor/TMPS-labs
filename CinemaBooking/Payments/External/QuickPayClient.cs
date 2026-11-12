namespace CinemaBooking.Payments.External;

// ---------------------------------------------------------------------------
// Pretend third-party SDK ("QuickPay"). Imagine this code comes from a NuGet
// package: we cannot change it, and it knows nothing about our cinema or our
// IPaymentMethod interface. It is the "Adaptee" of the Adapter pattern.
//
// Its API differs from ours in three ways:
//   - amounts are whole numbers of minor units (bani), not decimal MDL;
//   - it wants a currency code and a merchant reference on every call;
//   - it reports the outcome as a receipt with a status code, not a bool.
// ---------------------------------------------------------------------------

public sealed class QuickPayReceipt
{
    public QuickPayReceipt(string transactionId, int statusCode, string message)
    {
        TransactionId = transactionId;
        StatusCode = statusCode;
        Message = message;
    }

    public string TransactionId { get; }

    /// <summary>200 = approved, anything else = declined.</summary>
    public int StatusCode { get; }

    public string Message { get; }
}

public sealed class QuickPayClient
{
    /// <summary>The provider refuses single charges above 500.00 MDL.</summary>
    public const long MaxChargeMinorUnits = 50_000;

    private readonly string _merchantId;
    private int _counter;

    public QuickPayClient(string merchantId)
    {
        _merchantId = merchantId;
    }

    public QuickPayReceipt SubmitCharge(long amountMinorUnits, string currencyIso, string reference)
    {
        string transactionId = $"QP-{_merchantId}-{++_counter:0000}";

        if (amountMinorUnits <= 0)
            return new QuickPayReceipt(transactionId, 400, "Amount must be positive");

        if (currencyIso != "MDL")
            return new QuickPayReceipt(transactionId, 400, $"Unsupported currency {currencyIso}");

        if (amountMinorUnits > MaxChargeMinorUnits)
            return new QuickPayReceipt(transactionId, 402, "Declined: over the per-transaction limit");

        return new QuickPayReceipt(transactionId, 200, $"Approved ({reference})");
    }
}
