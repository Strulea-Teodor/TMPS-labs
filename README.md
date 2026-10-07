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
| Lab 1+| `Lab-N`  | Creational, structural and behavioral design patterns (9 total) |

Each lab lives on its own branch so the evolution of the design is easy to follow.

## Project structure

```
CinemaBooking/
├── Program.cs                      composition root + demo scenarios
├── Domain/                         plain data classes, no business logic
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

## Running the project

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
cd CinemaBooking
dotnet run
```

The demo runs three scenarios: a student booking two seats by card, a regular
customer booking on a Tuesday with PayPal, and an attempt to double-book a
seat that is correctly rejected.

## What comes next

The structure already leaves room for the patterns planned in later labs:
discounts are a natural **Strategy**, seat add-ons fit **Decorator**,
screening changes fit **Observer**, the booking lifecycle fits **State**,
and the composition root can grow into a **Factory** or **Builder**.
