# Laboratory Work 0 — SOLID Principles
# Laboratory Work 2 — Structural Design Patterns

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)
**Author:** Teodor Strulea, FAF-242
**Language:** C# (.NET 8)
**Project:** Cinema Booking System

## Objectives

1. Choose a simple domain for a semester-long pet project.
2. Study the SOLID principles.
3. Implement the project's core while applying three of the principles.

Principles applied in this laboratory work: **Single Responsibility (SRP)**,
**Open/Closed (OCP)** and **Dependency Inversion (DIP)**.

## Theory

SOLID is a set of five object-oriented design principles whose shared goal is
code that can change without breaking: each class has one job (S), new
behaviour is added by writing new code instead of editing working code (O),
subtypes can stand in for their base types (L), interfaces stay small (I), and
high-level logic depends on abstractions instead of concrete details (D).

## The project

A **cinema ticket booking system**. Customers pick a screening, choose seats,
get discounts applied, pay, and receive a ticket. The domain is deliberately
simple so that the focus stays on *how* the code is designed rather than
*what* it does.

```
CinemaBooking/
├── Program.cs                      composition root + demo scenarios
├── Domain/                         data classes that guard only their own invariants
│   ├── Movie.cs
│   ├── Screening.cs
│   ├── Seat.cs
│   ├── Customer.cs
│   └── Booking.cs
├── Discounts/                      OCP: pluggable pricing rules
│   ├── IDiscount.cs
│   ├── StudentDiscount.cs
│   └── TuesdayDiscount.cs
├── Payments/                       DIP: pluggable payment methods
1. Study the structural design patterns and understand what problem each one solves.
2. Choose three of them and implement them in the semester project.
3. Keep the existing code working: the new patterns must plug into the
   project instead of replacing it.

Patterns implemented in this laboratory work: **Adapter**, **Decorator** and **Facade**.

## Theory

Structural patterns describe how classes and objects are *composed* into
larger structures while keeping those structures flexible. Instead of
changing existing classes, they add a thin object between them: a translator
(Adapter), a wrapper that adds behaviour (Decorator), or a simplified front
door (Facade).

## The project

A cinema ticket booking system: customers pick a screening, choose seats, get
discounts applied, optionally add extras, pay, and receive a ticket.

```
CinemaBooking/
├── Program.cs                      composition root + demo of the three patterns
├── Domain/                         Movie, Screening, Seat, Customer, Booking
├── Halls/                          hall layouts that create the seats
├── Schedule/                       the cinema's screening schedule
├── Builders/                       BookingBuilder
├── Discounts/                      IDiscount and its implementations
├── Repositories/                   IBookingRepository, InMemoryBookingRepository
├── Services/                       PriceCalculator, TicketPrinter, BookingService
├── Payments/
│   ├── IPaymentMethod.cs
│   ├── CardPayment.cs
│   ├── PayPalPayment.cs
│   ├── QuickPayAdapter.cs          ADAPTER
│   └── External/
│       └── QuickPayClient.cs       the third-party SDK being adapted
├── Orders/                         DECORATOR
│   ├── IBookingOrder.cs            component (+ OrderLine)
│   ├── TicketsOrder.cs             concrete component
│   ├── OrderExtra.cs               base decorator
│   ├── PopcornExtra.cs             concrete decorators
│   ├── DrinkExtra.cs
│   ├── Glasses3DExtra.cs
│   └── Extra.cs                    Extra enum + ExtraCatalog
└── Facade/                         FACADE
    └── CinemaFacade.cs
```

## Implementation

### 1. Adapter — `QuickPayAdapter`

**Problem.** The cinema wants to accept payments through an external provider.
Its SDK (`QuickPayClient`, simulated in `Payments/External/`) cannot be
modified, and its API does not match what the application expects:

| The application expects (`IPaymentMethod`) | The SDK offers (`QuickPayClient`)                       |
|--------------------------------------------|---------------------------------------------------------|
| `bool Pay(decimal amount)`                 | `QuickPayReceipt SubmitCharge(long, string, string)`    |
| amount in MDL, as a `decimal`              | amount in bani, as a whole number                       |
| nothing else needed                        | a currency code and a merchant reference on every call  |
| success as `true` / `false`                | a receipt with a numeric status code                    |

`Booking` holds the booking data and nothing else. It does not know how to
price itself, store itself or print itself. Each of those concerns has its own
class:
Rewriting `BookingService` to call the SDK directly would tie the booking
flow to one provider.

**Solution.** `QuickPayAdapter` implements `IPaymentMethod` and holds a
`QuickPayClient`. It translates each call on the way in and the result on the
way out:

```csharp
public class QuickPayAdapter : IPaymentMethod
{
    private readonly QuickPayClient _client;
    private readonly string _reference;

    public bool Pay(decimal amount)
    {
        long minorUnits = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        QuickPayReceipt receipt = _client.SubmitCharge(minorUnits, "MDL", _reference);
        return receipt.StatusCode == 200;
    }
}
```

`BookingService` receives the adapter like any other payment method and
cannot tell the difference. This is the *object adapter* form: the adaptee is
a field, not a base class.

<p align="center"><img src="docs/uml/srp.png" alt="UML class diagram showing one responsibility per class" width="760"></p>

### 2. Open/Closed Principle (OCP)
<p align="center"><img src="docs/uml/adapter.png" alt="UML class diagram of the Adapter pattern" width="620"></p>

| Role    | Class             |
|---------|-------------------|
| Client  | `BookingService`  |
| Target  | `IPaymentMethod`  |
| Adapter | `QuickPayAdapter` |
| Adaptee | `QuickPayClient`  |

### 2. Decorator — order extras

**Problem.** Customers can add extras to a booking: popcorn, a drink, 3D
glasses, in any combination and quantity. Modelling this with inheritance
(`BookingWithPopcorn`, `BookingWithPopcornAndDrink`, ...) needs a class per
combination, and putting `bool hasPopcorn, int drinks, ...` fields into
`Booking` means editing it for every new product.

**Solution.** What the customer pays for is an `IBookingOrder`. The plain
tickets are one implementation (`TicketsOrder`). Each extra is another
implementation that *wraps* an `IBookingOrder` and adds its own line and
price on top:

```csharp
public abstract class OrderExtra : IBookingOrder
{
    private readonly IBookingOrder _inner;

    protected abstract string Label { get; }
    protected abstract decimal ExtraPrice { get; }

    public Booking Booking => _inner.Booking;
    public IReadOnlyList<OrderLine> Lines => _inner.Lines.Append(new OrderLine(Label, ExtraPrice)).ToList();
    public decimal Total => _inner.Total + ExtraPrice;
}

public class PopcornExtra : OrderExtra
{
    public PopcornExtra(IBookingOrder inner) : base(inner) { }
    protected override string Label => "Large popcorn";
    protected override decimal ExtraPrice => 60m;
}
```

`PriceCalculator` simply iterates over the discounts it is given:

```csharp
foreach (var discount in _discounts)
{
    if (discount.IsApplicable(booking))
        price = discount.Apply(booking, price);
}
```

To add a new rule — say a `SeniorDiscount` — you write a new class and
register it in `Program.cs`. `PriceCalculator` is never edited.

<p align="center"><img src="docs/uml/ocp.png" alt="UML class diagram of PriceCalculator and the IDiscount hierarchy" width="700"></p>
Extras are stacked at run time, in any order, and the same one can be applied
more than once:

```csharp
IBookingOrder order = new TicketsOrder(booking, ticketsPrice);
order = new PopcornExtra(order);
order = new DrinkExtra(order);
order = new DrinkExtra(order);
order = new Glasses3DExtra(order);   // priced per seat, read from the wrapped order
```

`BookingService` and `TicketPrinter` work only with `IBookingOrder`, so they
handle an order with zero extras or ten extras with the same code. A new
extra is one small class; nothing existing changes.

<p align="center"><img src="docs/uml/decorator.png" alt="UML class diagram of the Decorator pattern" width="700"></p>

| Role                | Class                                           |
|---------------------|-------------------------------------------------|
| Component           | `IBookingOrder`                                 |
| Concrete component  | `TicketsOrder`                                  |
| Decorator           | `OrderExtra`                                    |
| Concrete decorators | `PopcornExtra`, `DrinkExtra`, `Glasses3DExtra`  |

### 3. Facade — `CinemaFacade`

**Problem.** Buying a ticket takes seven collaborating objects in a fixed
sequence: find the screening in `CinemaSchedule`, assemble the booking with
`BookingBuilder`, price it with `PriceCalculator`, wrap it in extras, confirm
it through `BookingService` (which pays and saves through
`IBookingRepository`), and print it with `TicketPrinter`. Every client
(the console demo now, a GUI or web API later) would have to repeat that
sequence and know all of those classes.

**Solution.** `CinemaFacade` knows the sequence and exposes it as one call:

```csharp
var cinema = new CinemaFacade(priceCalculator, repository, printer);

cinema.BookTickets(
    maria,
    1,                                      // screening id
    new[] { "B3", "C3" },                   // seats
    new CardPayment("4111222233334444"),    // any IPaymentMethod
    Extra.Popcorn, Extra.Drink, Extra.Glasses3D);
```

It has no idea whether the money comes from a card or PayPal, or whether
bookings live in memory or in SQL. The concrete classes are chosen in one
place (`Program.cs`, the composition root) and injected. The demo runs the
same `BookingService` class once with `CardPayment` and once with
`PayPalPayment` without a single line of the service changing.

<p align="center"><img src="docs/uml/dip.png" alt="UML class diagram showing BookingService depending on interfaces" width="700"></p>

## Results

It also offers `ShowProgramme()`, `ShowSeatMap(id)` and `ShowSummary()`. The
facade contains no business rules of its own; it only coordinates. Errors from
the subsystem (unknown screening, taken seat, failed payment) pass through
unchanged, and the subsystem classes stay public for anything the facade does
not cover.

<p align="center"><img src="docs/uml/facade.png" alt="UML class diagram of the Facade pattern" width="760"></p>

| Role      | Class                                                                                                        |
|-----------|--------------------------------------------------------------------------------------------------------------|
| Client    | `Program`                                                                                                    |
| Facade    | `CinemaFacade`                                                                                               |
| Subsystem | `CinemaSchedule`, `BookingBuilder`, `PriceCalculator`, `ExtraCatalog`, `BookingService`, `IBookingRepository`, `TicketPrinter` |

### How the three work together

In one `BookTickets` call the facade builds a `TicketsOrder`, wraps it in the
requested decorators, and hands it to `BookingService` together with a
payment method that may be the QuickPay adapter. Each pattern hides one kind
of detail from the others: the facade hides the *sequence*, the decorator
hides *how many extras* there are, and the adapter hides *whose API* takes the
money.

## Results

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer.

```bash
cd CinemaBooking
dotnet run
```

The demo runs three scenarios: a student booking two seats by card, a regular
customer booking on a Tuesday with PayPal, and an attempt to double-book a
seat that is correctly rejected.

## Conclusions

- **SRP** made every class short and easy to name: when a requirement
  changes, there is one obvious place to edit.
- **OCP** turned "add a discount" from an edit inside the pricing logic into
  a new, isolated class.
- **DIP** decoupled the booking flow from payment and storage details, so
  either can be replaced without touching `BookingService`.
- Together they give the project a base that can grow in small steps without
  rewriting what already works.
The demo has three parts:

1. **Decorator** — wraps a tickets order one extra at a time and prints the
   total after each layer, then the full list of receipt lines.
2. **Adapter** — calls the SDK directly to show its API, pays for a booking
   through `QuickPayAdapter`, then attempts a booking above the provider's
   500 MDL limit: the SDK returns status 402, the adapter turns it into
   `false`, and the seats stay free.
3. **Facade** — books two seats with three extras in a single call, prints the
   seat map, and shows two invalid requests being rejected.

## Conclusions

- **Adapter** let the project use a third-party payment API without changing
  either side. The cost is one small class, and replacing the provider later
  means writing another adapter, not touching the booking flow.
- **Decorator** replaced a potential explosion of subclasses or flags with
  small wrappers that are combined at run time. It works because every wrapper
  keeps the interface of what it wraps.
- **Facade** reduced the client's knowledge of the system from seven classes
  and their call order to one method. It makes the common case easy without
  hiding the subsystem from code that needs more control.
- All three patterns relied on abstractions already in the project
  (`IPaymentMethod`, `IBookingRepository`), which is what made it possible to
  add them without rewriting existing classes.
