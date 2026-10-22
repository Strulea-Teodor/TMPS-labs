# TMPS Labs — Cinema Booking System

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)
**Author:** Teodor Strulea, FAF-242
**Language:** C# (.NET 8)

## About the project

This repository holds a single pet project that grows over the whole semester:
a **cinema ticket booking system**. Customers pick a screening, choose seats,
get discounts applied, pay, and receive a ticket.

The domain is deliberately simple so that the focus stays on *how* the code is
designed rather than *what* it does. Each lab adds a new layer of design to the
same codebase:

| Lab   | Branch   | Topic                                                           |
|-------|----------|-----------------------------------------------------------------|
| Lab 0 | `Lab-0`  | Project setup + 3 SOLID principles (SRP, OCP, DIP)              |
| Lab 1 | `Lab-1`  | Creational patterns: Singleton, Factory Method, Builder         |
| Lab 2+| `Lab-N`  | Structural and behavioral design patterns                       |

Each lab lives on its own branch so the evolution of the design is easy to follow.

## Project structure

```
CinemaBooking/
├── Program.cs                      composition root + demo scenarios
├── Domain/                         plain data classes, no business logic
│   ├── Movie.cs
│   ├── Screening.cs
│   ├── Seat.cs                     abstract product (Factory Method)
│   ├── StandardSeat.cs             concrete product
│   ├── VipSeat.cs                  concrete product
│   ├── Customer.cs
│   └── Booking.cs
├── Halls/                          FACTORY METHOD
│   ├── HallLayout.cs               abstract creator with CreateSeat()
│   ├── StandardHallLayout.cs       concrete creator
│   └── PremiumHallLayout.cs        concrete creator
├── Schedule/                       SINGLETON
│   └── CinemaSchedule.cs
├── Builders/                       BUILDER
│   └── BookingBuilder.cs
├── Discounts/                      OCP: pluggable pricing rules
│   ├── IDiscount.cs
│   ├── StudentDiscount.cs
│   └── TuesdayDiscount.cs
├── Payments/                       DIP: pluggable payment methods
│   ├── IPaymentMethod.cs
│   ├── CardPayment.cs
│   └── PayPalPayment.cs
├── Repositories/                   DIP: pluggable storage
│   ├── IBookingRepository.cs
│   └── InMemoryBookingRepository.cs
└── Services/                       SRP: one job per class
    ├── PriceCalculator.cs
    ├── TicketPrinter.cs
    └── BookingService.cs
```

## Lab 0 — SOLID principles

### 1. Single Responsibility Principle (SRP)

> A class should have only one reason to change.

`Booking` is a pure data holder. It does not know how to price itself, store
itself or print itself. Each of those concerns has its own class:

| Class                       | Its one job                                  |
|-----------------------------|----------------------------------------------|
| `Booking`                   | hold the customer, screening and seats       |
| `PriceCalculator`           | work out the total cost                      |
| `InMemoryBookingRepository` | store and retrieve bookings                  |
| `TicketPrinter`             | format and print a ticket                    |
| `BookingService`            | orchestrate the booking flow                 |

If the ticket layout changes, only `TicketPrinter` is touched. If storage
moves to a database, only the repository changes.

### 2. Open/Closed Principle (OCP)

> Software entities should be open for extension but closed for modification.

Discounts are expressed through the `IDiscount` interface:

```csharp
public interface IDiscount
{
    string Name { get; }
    bool IsApplicable(Booking booking);
    decimal Apply(Booking booking, decimal currentPrice);
}
```

`PriceCalculator` simply iterates over the discounts it is given. To add a
new rule — say a `SeniorDiscount` or a `PremiereSurcharge` — you write a new
class and register it in `Program.cs`. `PriceCalculator` is never edited.

### 3. Dependency Inversion Principle (DIP)

> High-level modules should not depend on low-level modules; both should
> depend on abstractions.

`BookingService` is the high-level policy. It receives everything it needs
through its constructor as abstractions:

```csharp
public BookingService(
    PriceCalculator priceCalculator,
    IBookingRepository repository,
    IPaymentMethod paymentMethod)
```

It has no idea whether the money comes from a card or PayPal, or whether
bookings live in memory or in SQL. The concrete classes are chosen in one
place (`Program.cs`, the composition root) and injected. The demo shows the
same service running with `CardPayment` and then with `PayPalPayment`
without any change to the service.

## Lab 1 — Creational patterns

Creational patterns deal with *how objects get created*. Three of them were
chosen because each solves a real problem that the Lab 0 code already had.

### 1. Singleton — `CinemaSchedule`

**Problem.** The cinema has exactly one timetable. If two parts of the app
each built their own list of screenings, they would also each have their own
copy of the seats, and two customers could end up with the same seat.

**Solution.** `CinemaSchedule` can only be reached through
`CinemaSchedule.Instance`:

```csharp
public sealed class CinemaSchedule
{
    private static readonly Lazy<CinemaSchedule> _instance = new(() => new CinemaSchedule());
    public static CinemaSchedule Instance => _instance.Value;

    private CinemaSchedule() { }
    ...
}
```

- the **private constructor** makes `new CinemaSchedule()` a compile error
  anywhere else;
- **`Lazy<T>`** creates the instance on first use and is thread-safe by
  default, so no hand-written locking is needed;
- **`sealed`** prevents a subclass from smuggling in a second instance.

The demo fetches `Instance` twice and shows `ReferenceEquals` is `true`.

### 2. Factory Method — `HallLayout.CreateSeat()`

**Problem.** In Lab 0 `Screening` built its seats with `new Seat(...)` in a
loop. Introducing VIP seats would have meant putting `if (isVipRow)` logic
inside `Screening`, and every new seat type would grow that `if` chain.

**Solution.** Seat creation is delegated to a *creator* hierarchy:

```
HallLayout (abstract)                      Seat (abstract)
 ├─ GenerateSeats()   ── uses ──►  CreateSeat()  ──creates──►  ├─ StandardSeat
 ├─ StandardHallLayout : CreateSeat() → StandardSeat            └─ VipSeat
 └─ PremiumHallLayout  : CreateSeat() → VipSeat for the last N rows
```

`HallLayout.GenerateSeats()` holds the shared algorithm (walk rows ×
numbers) and calls the abstract `CreateSeat(row, number)` for each position.
Each subclass overrides only that one method. `Screening` just receives a
`HallLayout` and asks it for seats; it never names a concrete seat class.

A side benefit: each `Seat` subclass carries its own `PriceMultiplier`, so
`PriceCalculator` prices a VIP seat correctly without knowing VIP exists.

### 3. Builder — `BookingBuilder`

**Problem.** A booking is assembled gradually: the customer is known first,
then the screening, then seats are clicked one at a time, then maybe a note.
In Lab 0 all of this was squeezed into `BookingService.Book(customer,
screening, seatLabels)`, mixing *construction* with *processing*.

**Solution.** A fluent builder collects the parts and validates them once,
in `Build()`:

```csharp
var booking = new BookingBuilder()
    .ForCustomer(ana)
    .ForScreening(schedule.FindById(1)!)
    .AddSeat("B3")
    .AddSeat("C3")
    .WithNote("Birthday outing")
    .Build();          // throws if anything is missing or a seat is invalid

bookingService.Confirm(booking);   // price -> pay -> reserve -> save
```

`Build()` checks that a customer and a screening were set, that at least one
seat was added, that no seat was added twice, that every seat exists in the
hall, and that none is already taken. A `Booking` object therefore can never
exist in a half-built or invalid state. `BookingService` shrank to its real
job: pricing, payment, reservation and persistence.

### Patterns considered but not used (yet)

- **Abstract Factory** would make sense once halls need *families* of
  related objects (seats + screen + sound system), not just seats.
- **Prototype** fits cloning a template `Screening` across a week of
  identical time slots - a good candidate for a later lab.
- **Object Pool** is unnecessary here; seat objects are cheap to create.

## Running the project

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
cd CinemaBooking
dotnet run
```

The demo walks through the three patterns in order: proves the schedule is a
single shared instance, prints the seat types each hall layout produced, then
builds and confirms two bookings (one with a VIP seat and a student discount,
one on a Tuesday via PayPal) and shows four invalid bookings being rejected
by the builder.

## What comes next

The structure already leaves room for the patterns planned in later labs:
discounts are a natural **Strategy**, seat add-ons fit **Decorator**,
screening changes fit **Observer**, the booking lifecycle fits **State**,
and `BookingService` could become a **Facade** over the whole flow.
