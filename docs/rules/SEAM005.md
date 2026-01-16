# SEAM005: DateTime.Now/UtcNow Creates Non-Deterministic Dependency

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM005 |
| **Category** | StaticDependencies |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects usage of `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now`, and `DateTimeOffset.UtcNow` which create non-deterministic dependencies making testing difficult.

## Why This Is Problematic

Direct time access causes serious testing challenges:

1. **Non-Deterministic Results**: Tests can pass or fail depending on when they run
2. **Time-Sensitive Logic**: Business logic based on time becomes untestable
3. **Race Conditions**: Tests checking timestamps may fail due to execution timing
4. **Timezone Issues**: `DateTime.Now` behavior varies by machine timezone
5. **Cannot Test Edge Cases**: Testing year-end, month-end, leap years, DST transitions requires waiting or hacks
6. **Flaky Tests**: Time-dependent assertions often fail intermittently

## Examples

### Non-Compliant Code

```csharp
public class InvoiceService
{
    public Invoice CreateInvoice(Order order)
    {
        return new Invoice
        {
            OrderId = order.Id,
            // Bad: Direct DateTime.Now usage
            CreatedAt = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30)
        };
    }
}
```

```csharp
public class SubscriptionService
{
    public bool IsExpired(Subscription subscription)
    {
        // Bad: Direct DateTime.UtcNow comparison
        return subscription.ExpiresAt < DateTime.UtcNow;
    }

    public Subscription Renew(Subscription subscription)
    {
        // Bad: Using DateTime.Today
        subscription.ExpiresAt = DateTime.Today.AddYears(1);
        return subscription;
    }
}
```

```csharp
public class AuditLogger
{
    public void Log(string action, string userId)
    {
        // Bad: DateTimeOffset.Now usage
        var entry = new AuditEntry
        {
            Timestamp = DateTimeOffset.Now,
            Action = action,
            UserId = userId
        };
        Save(entry);
    }
}
```

### Compliant Code

Using .NET 8+ `TimeProvider`:

```csharp
public class InvoiceService
{
    private readonly TimeProvider _timeProvider;

    public InvoiceService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Invoice CreateInvoice(Order order)
    {
        var now = _timeProvider.GetUtcNow();
        return new Invoice
        {
            OrderId = order.Id,
            CreatedAt = now.DateTime,
            DueDate = now.DateTime.AddDays(30)
        };
    }
}

// In DI registration
services.AddSingleton(TimeProvider.System);

// In tests
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero));
var service = new InvoiceService(fakeTime);
```

Using custom interface (pre-.NET 8):

```csharp
public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateTime Today { get; }
}

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
}

public class SubscriptionService
{
    private readonly IDateTimeProvider _dateTime;

    public SubscriptionService(IDateTimeProvider dateTime)
    {
        _dateTime = dateTime;
    }

    public bool IsExpired(Subscription subscription)
    {
        return subscription.ExpiresAt < _dateTime.UtcNow;
    }
}
```

Using `IClock` pattern:

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

// Test implementation
public class FixedClock : IClock
{
    public FixedClock(DateTimeOffset fixedTime) => UtcNow = fixedTime;
    public DateTimeOffset UtcNow { get; set; }
}
```

## How to Fix

1. **Choose a Time Abstraction**: Use .NET 8+ `TimeProvider` or create `IDateTimeProvider`/`IClock`
2. **Inject the Dependency**: Add the time provider as a constructor parameter
3. **Replace Direct Calls**: Change `DateTime.Now` to `_timeProvider.Now`
4. **Register in DI**: Add the system implementation to your container
5. **Create Test Implementation**: Use `FakeTimeProvider` or custom test doubles

### Migration Example

```csharp
// Before
public bool IsWithinBusinessHours()
{
    var now = DateTime.Now;
    return now.Hour >= 9 && now.Hour < 17;
}

// After
public bool IsWithinBusinessHours()
{
    var now = _timeProvider.GetLocalNow();
    return now.Hour >= 9 && now.Hour < 17;
}

// Test
[Fact]
public void IsWithinBusinessHours_At10AM_ReturnsTrue()
{
    var time = new FakeTimeProvider(
        new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.FromHours(-5)));
    var service = new BusinessService(time);

    Assert.True(service.IsWithinBusinessHours());
}

[Fact]
public void IsWithinBusinessHours_At8PM_ReturnsFalse()
{
    var time = new FakeTimeProvider(
        new DateTimeOffset(2024, 1, 15, 20, 0, 0, TimeSpan.FromHours(-5)));
    var service = new BusinessService(time);

    Assert.False(service.IsWithinBusinessHours());
}
```

## When to Suppress

Suppression is appropriate when:

- The code is **logging/tracing** where exact time accuracy in tests isn't important
- You're in **startup/configuration** code that runs once
- The timestamp is for **display only** and not used in logic
- You're working with **legacy code** with a plan to refactor

```csharp
#pragma warning disable SEAM005
// Startup logging - not business logic
Console.WriteLine($"Application started at {DateTime.Now}");
#pragma warning restore SEAM005
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM005.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM005.severity = suggestion
```

## Related Rules

- [SEAM006](SEAM006.md) - Guid.NewGuid (similar non-determinism)
- [SEAM004](SEAM004.md) - Static Method Calls (general pattern)
- [SEAM008](SEAM008.md) - Static Property Access (related pattern)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [TimeProvider in .NET 8](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider)
- [FakeTimeProvider for Testing](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.time.testing.faketimeprovider)
- [NodaTime Clock Abstraction](https://nodatime.org/3.1.x/userguide/testing)
