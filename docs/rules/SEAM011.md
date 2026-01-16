# SEAM011: Complex Private Method Should Be Extracted

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM011 |
| **Category** | InheritanceBlockers |
| **Severity** | Info |
| **Enabled** | No (opt-in) |

## Description

Detects large private methods (default: 50+ lines) that contain logic which cannot be tested in isolation. These methods should be extracted to separate testable classes.

## Why This Rule Is Disabled by Default

This rule is disabled by default because:

1. **Threshold Sensitivity**: The "right" size varies by project and context
2. **Subjectivity**: What constitutes "complex" is debatable
3. **Noise Potential**: May flag methods that are intentionally private
4. **Refactoring Effort**: Extracting classes is significant work

Enable this rule when you want to identify candidates for extraction in legacy code.

## Why This Is Problematic

Large private methods cause testing challenges:

1. **Cannot Test Directly**: Private methods aren't accessible to test classes
2. **Hidden Logic**: Complex algorithms are buried inside public method tests
3. **Low Coverage**: Important logic may not be thoroughly tested
4. **Difficult Debugging**: Hard to isolate failures in large methods
5. **Code Reuse**: Logic locked in private methods can't be reused
6. **Single Responsibility**: Large methods often do too many things

## Examples

### Non-Compliant Code

```csharp
public class OrderProcessor
{
    public ProcessingResult ProcessOrder(Order order)
    {
        if (!ValidateBasicInfo(order))
            return ProcessingResult.Invalid;

        var total = CalculateOrderTotal(order); // 80-line private method
        return new ProcessingResult { Total = total, Status = "Processed" };
    }

    // Bad: Complex private method with 80+ lines
    private decimal CalculateOrderTotal(Order order)
    {
        decimal subtotal = 0;

        foreach (var item in order.Items)
        {
            var basePrice = item.UnitPrice;

            // Volume discount logic (20 lines)
            if (item.Quantity >= 100)
                basePrice *= 0.85m;
            else if (item.Quantity >= 50)
                basePrice *= 0.90m;
            else if (item.Quantity >= 20)
                basePrice *= 0.95m;

            // Seasonal pricing (15 lines)
            if (IsHolidaySeason())
            {
                // Complex seasonal pricing rules...
            }

            // Customer tier discount (15 lines)
            var customerDiscount = GetCustomerTierDiscount(order.CustomerId);

            // Promotional codes (20 lines)
            foreach (var promo in order.PromoCodes)
            {
                // Complex promo code validation and application...
            }

            // Tax calculation (15 lines)
            var taxRate = GetTaxRate(order.ShippingAddress);

            subtotal += /* complex calculation */;
        }

        // Shipping calculation (20 lines)
        // ... more complex logic

        return subtotal;
    }
}
```

### Compliant Code

```csharp
public class OrderProcessor
{
    private readonly IOrderCalculator _calculator;

    public OrderProcessor(IOrderCalculator calculator)
    {
        _calculator = calculator;
    }

    public ProcessingResult ProcessOrder(Order order)
    {
        if (!ValidateBasicInfo(order))
            return ProcessingResult.Invalid;

        var total = _calculator.CalculateTotal(order);
        return new ProcessingResult { Total = total, Status = "Processed" };
    }
}

// Extracted to separate testable class
public interface IOrderCalculator
{
    decimal CalculateTotal(Order order);
}

public class OrderCalculator : IOrderCalculator
{
    private readonly IDiscountService _discountService;
    private readonly ITaxService _taxService;
    private readonly IShippingCalculator _shippingCalculator;

    public OrderCalculator(
        IDiscountService discountService,
        ITaxService taxService,
        IShippingCalculator shippingCalculator)
    {
        _discountService = discountService;
        _taxService = taxService;
        _shippingCalculator = shippingCalculator;
    }

    public decimal CalculateTotal(Order order)
    {
        var subtotal = CalculateSubtotal(order);
        var discount = _discountService.Calculate(order);
        var tax = _taxService.Calculate(subtotal - discount, order.ShippingAddress);
        var shipping = _shippingCalculator.Calculate(order);

        return subtotal - discount + tax + shipping;
    }

    // Now each component is testable
    private decimal CalculateSubtotal(Order order)
    {
        return order.Items.Sum(i => i.UnitPrice * i.Quantity);
    }
}

// Each service is independently testable
public class VolumeDiscountService : IDiscountService
{
    public decimal Calculate(Order order)
    {
        // Volume discount logic - now testable in isolation
    }
}
```

## How to Fix

1. **Identify Responsibility**: Determine what the private method is responsible for
2. **Extract Interface**: Define an interface for the capability
3. **Create Class**: Move the logic to a new public class
4. **Inject Dependency**: Add the new class as a constructor parameter
5. **Write Tests**: Create unit tests for the extracted class
6. **Simplify Original**: The calling method becomes simpler

### Extraction Strategy

```csharp
// Step 1: Identify the complex private method
private decimal CalculateOrderTotal(Order order) { /* 80 lines */ }

// Step 2: Define interface
public interface IOrderCalculator
{
    decimal CalculateTotal(Order order);
}

// Step 3: Create implementation class
public class OrderCalculator : IOrderCalculator
{
    public decimal CalculateTotal(Order order)
    {
        // Move logic here, now it's public and testable
    }
}

// Step 4: Inject and use
public class OrderProcessor
{
    private readonly IOrderCalculator _calculator;

    public OrderProcessor(IOrderCalculator calculator)
    {
        _calculator = calculator;
    }

    public ProcessingResult ProcessOrder(Order order)
    {
        var total = _calculator.CalculateTotal(order);
        // ...
    }
}

// Step 5: Write tests
[Fact]
public void CalculateTotal_WithVolumeDiscount_AppliesDiscount()
{
    var calculator = new OrderCalculator();
    var order = new Order { Items = { new OrderItem { Quantity = 100, UnitPrice = 10 } } };

    var total = calculator.CalculateTotal(order);

    Assert.Equal(850m, total); // 15% volume discount
}
```

## How to Enable

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM011.severity = suggestion

# Configure the line threshold (default: 50)
dotnet_code_quality.SEAM011.complexity_threshold = 30

# Exclude specific methods
dotnet_code_quality.SEAM011.excluded_methods = LegacyCalculation
```

## When to Suppress

Suppression is appropriate when:

- The method is **intentionally private** and tested via public methods
- The method contains **generated code** or templates
- Extraction would create **unnecessary complexity**
- The code is **legacy** with a planned rewrite

```csharp
#pragma warning disable SEAM011
// This is tested via ProcessOrder - extraction would over-complicate
private decimal CalculateOrderTotal(Order order)
{
    // Large but cohesive method
}
#pragma warning restore SEAM011
```

## Configuration

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM011.severity = suggestion

# Set custom line threshold
dotnet_code_quality.SEAM011.complexity_threshold = 40

# Exclude specific methods
dotnet_code_quality.SEAM011.excluded_methods = GeneratedMethod, LegacyProcess
```

## Related Rules

- [SEAM009](SEAM009.md) - Sealed Classes
- [SEAM010](SEAM010.md) - Non-Virtual Methods
- [SEAM001](SEAM001.md) - Direct Instantiation

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers - "Break Out Method Object" and "Extract Class"
- [Refactoring: Improving the Design of Existing Code](https://www.amazon.com/Refactoring-Improving-Design-Existing-Code/dp/0201485672) by Martin Fowler
- [Extract Class Refactoring](https://refactoring.guru/extract-class)
