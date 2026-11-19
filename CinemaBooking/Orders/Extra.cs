namespace CinemaBooking.Orders;

/// <summary>
/// The extras a customer can ask for. Used by CinemaFacade so that client
/// code can request extras without knowing the decorator classes.
/// </summary>
public enum Extra
{
    Popcorn,
    Drink,
    Glasses3D,
}

public static class ExtraCatalog
{
    /// <summary>Wraps the order in the decorator that matches the requested extra.</summary>
    public static IBookingOrder Add(IBookingOrder order, Extra extra) => extra switch
    {
        Extra.Popcorn   => new PopcornExtra(order),
        Extra.Drink     => new DrinkExtra(order),
        Extra.Glasses3D => new Glasses3DExtra(order),
        _ => throw new ArgumentOutOfRangeException(nameof(extra), extra, "Unknown extra."),
    };
}
