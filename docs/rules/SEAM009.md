# SEAM009: Sealed Class Prevents Inheritance Seam

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM009 |
| **Category** | InheritanceBlockers |
| **Severity** | Info |
| **Enabled** | No (opt-in) |

## Description

Detects public sealed classes that cannot be subclassed for testing purposes. The "subclass and override" technique described in "Working Effectively with Legacy Code" requires classes to be unsealed.

## Why This Rule Is Disabled by Default

This rule is disabled by default because:

1. **Sealed by Design**: Many classes are intentionally sealed for security or design reasons
2. **Interface Preference**: Using interfaces is usually preferred over inheritance for testing
3. **Modern Mocking**: Tools like MOQ don't require subclassing for interface mocks
4. **Performance**: JIT can optimize sealed classes better
5. **Framework Guidance**: Microsoft recommends sealing classes without virtual members

Enable this rule when you need to use subclass-and-override techniques for legacy code.

## Detection Logic

The analyzer flags sealed classes when:
- The class is `public` or `protected`
- The class has instance methods (not just properties/data)
- The class is NOT a record type
- The class name doesn't end with `Exception`, `Attribute`, or `EventArgs`

## Why This Is Problematic

In legacy code testing scenarios:

1. **Cannot Subclass**: The "subclass and override" testing technique is blocked
2. **No Override Seams**: Cannot override specific methods to break dependencies
3. **Limited Testing Options**: Must use interfaces or reflection-based mocking
4. **Sensing Difficulty**: Cannot override methods to capture/verify behavior

However, modern C# practices often favor:
- **Interfaces over inheritance** for abstraction
- **Composition over inheritance** for flexibility
- **Sealed classes** for better performance and clear intent

## Examples

### Non-Compliant Code

```csharp
// Bad for legacy testing: Sealed prevents subclass-and-override
public sealed class OrderCalculator
{
    public decimal CalculateTotal(Order order)
    {
        var subtotal = CalculateSubtotal(order);
        var tax = CalculateTax(subtotal);
        var shipping = CalculateShipping(order);
        return subtotal + tax + shipping;
    }

    public decimal CalculateSubtotal(Order order) { /* ... */ }
    public decimal CalculateTax(decimal amount) { /* ... */ }
    public decimal CalculateShipping(Order order) { /* ... */ }
}
```

### Compliant Code

Using interface (preferred approach):

```csharp
public interface IOrderCalculator
{
    decimal CalculateTotal(Order order);
    decimal CalculateSubtotal(Order order);
    decimal CalculateTax(decimal amount);
    decimal CalculateShipping(Order order);
}

// Class can remain sealed since interface enables mocking
public sealed class OrderCalculator : IOrderCalculator
{
    public decimal CalculateTotal(Order order) { /* ... */ }
    public decimal CalculateSubtotal(Order order) { /* ... */ }
    public decimal CalculateTax(decimal amount) { /* ... */ }
    public decimal CalculateShipping(Order order) { /* ... */ }
}
```

Removing sealed for subclass-and-override:

```csharp
// Unsealed allows subclass-and-override testing
public class OrderCalculator
{
    public virtual decimal CalculateTotal(Order order)
    {
        var subtotal = CalculateSubtotal(order);
        var tax = CalculateTax(subtotal);
        var shipping = CalculateShipping(order);
        return subtotal + tax + shipping;
    }

    // Virtual methods can be overridden in tests
    public virtual decimal CalculateTax(decimal amount) { /* ... */ }
    public virtual decimal CalculateShipping(Order order) { /* ... */ }
}

// Test subclass
public class TestableOrderCalculator : OrderCalculator
{
    public decimal FixedTax { get; set; } = 10m;
    public decimal FixedShipping { get; set; } = 5m;

    public override decimal CalculateTax(decimal amount) => FixedTax;
    public override decimal CalculateShipping(Order order) => FixedShipping;
}
```

### Acceptable Sealed Classes

```csharp
// OK: Exception types should remain sealed
public sealed class OrderValidationException : Exception { }

// OK: Attribute types should remain sealed
public sealed class ValidOrderAttribute : Attribute { }

// OK: EventArgs should remain sealed
public sealed class OrderCreatedEventArgs : EventArgs { }

// OK: Data class with no behavior (only properties)
public sealed class OrderDto
{
    public int Id { get; set; }
    public decimal Total { get; set; }
}
```

## How to Fix

Consider your testing strategy:

1. **Prefer Interfaces**: Extract interface and mock that instead
2. **Remove Sealed**: If subclass-and-override is needed, remove the `sealed` modifier
3. **Add Virtual**: Make methods `virtual` to enable overriding
4. **Use Composition**: Inject dependencies that can be mocked

### Interface Extraction

```csharp
// Step 1: Extract interface from sealed class
public interface IOrderCalculator
{
    decimal CalculateTotal(Order order);
}

// Step 2: Implement interface (class can stay sealed)
public sealed class OrderCalculator : IOrderCalculator
{
    public decimal CalculateTotal(Order order) { /* ... */ }
}

// Step 3: Depend on interface
public class OrderService
{
    private readonly IOrderCalculator _calculator;

    public OrderService(IOrderCalculator calculator)
    {
        _calculator = calculator;
    }
}

// Step 4: Mock interface in tests
var mockCalculator = new Mock<IOrderCalculator>();
mockCalculator.Setup(c => c.CalculateTotal(It.IsAny<Order>()))
    .Returns(100m);
```

## How to Enable

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM009.severity = suggestion

# Exclude specific namespaces
dotnet_code_quality.SEAM009.excluded_namespaces = MyApp.Dtos, MyApp.Events

# Exclude specific types
dotnet_code_quality.SEAM009.excluded_types = MyLegacyClass
```

## When to Suppress

Suppression is appropriate when:

- The class implements an **interface** that's used for mocking
- The class is **intentionally sealed** for security reasons
- The class is a **pure data object** with no behavior
- You have **alternative testing strategies** that don't require inheritance

```csharp
#pragma warning disable SEAM009
// Intentionally sealed - use IOrderCalculator interface for testing
public sealed class OrderCalculator : IOrderCalculator
{
}
#pragma warning restore SEAM009
```

## Configuration

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM009.severity = suggestion

# Exclude specific namespaces
dotnet_code_quality.SEAM009.excluded_namespaces = MyApp.Dtos

# Exclude specific types
dotnet_code_quality.SEAM009.excluded_types = SpecialClass
```

## Related Rules

- [SEAM010](SEAM010.md) - Non-Virtual Methods
- [SEAM002](SEAM002.md) - Concrete Constructor Parameters
- [SEAM011](SEAM011.md) - Complex Private Methods

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers - Chapter "The Seam Model"
- [Sealed Classes](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/sealed)
- [CA1852: Seal internal types](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1852) - Microsoft's recommendation
