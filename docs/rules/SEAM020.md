# SEAM020: Method Has High Cyclomatic Complexity

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM020 |
| **Category** | Complexity |
| **Severity** | Warning |
| **Enabled** | Yes |

## Description

Detects methods with cyclomatic complexity exceeding a configurable threshold (default: 25). Cyclomatic complexity measures the number of linearly independent paths through a method's source code.

## Why This Is Problematic

High cyclomatic complexity causes testing and maintenance challenges:

1. **Difficult to Test**: Each path requires a separate test case, making thorough testing impractical
2. **Hard to Understand**: More paths mean more mental overhead to comprehend the code
3. **Error Prone**: Complex code is more likely to contain bugs
4. **Difficult to Maintain**: Changes risk breaking existing paths
5. **Poor Code Coverage**: Achieving high coverage becomes exponentially harder

## Decision Points Counted

Each of the following constructs adds +1 to the complexity:

| Construct | Description |
|-----------|-------------|
| `if`/`else if` | Conditional branch |
| `case` (switch statement) | Switch case label |
| `case` (pattern matching) | Pattern matching case |
| Switch expression arm | Each arm (except discard `_`) |
| `for` loop | Loop iteration |
| `foreach` loop | Loop iteration |
| `while` loop | Loop condition |
| `do-while` loop | Loop condition |
| `catch` block | Exception handler |
| `&&` | Logical AND operator |
| `||` | Logical OR operator |
| `?:` | Ternary conditional |
| `??` | Null-coalescing operator |

**Not counted:**
- Lambdas (analyzed separately)
- Local functions (analyzed separately)
- Anonymous methods (analyzed separately)
- Discard patterns (`_` in switch expressions)

## Examples

### Non-Compliant Code

```csharp
public class OrderProcessor
{
    // SEAM020: Method 'ProcessOrder' has cyclomatic complexity of 28 (threshold: 25)
    public ProcessingResult ProcessOrder(Order order)
    {
        if (order == null) return ProcessingResult.Invalid;
        if (!order.IsActive) return ProcessingResult.Inactive;

        decimal total = 0;

        foreach (var item in order.Items)
        {
            if (item.Quantity <= 0) continue;

            var price = item.UnitPrice;

            // Volume discounts
            if (item.Quantity >= 100)
                price *= 0.85m;
            else if (item.Quantity >= 50)
                price *= 0.90m;
            else if (item.Quantity >= 20)
                price *= 0.95m;

            // Category discounts
            switch (item.Category)
            {
                case "Electronics":
                    price *= 0.95m;
                    break;
                case "Books":
                    price *= 0.90m;
                    break;
                case "Clothing":
                    if (IsSalesSeason())
                        price *= 0.80m;
                    break;
            }

            // Customer tier
            if (order.CustomerTier == "Gold" && item.IsEligibleForTierDiscount)
                price *= 0.95m;
            else if (order.CustomerTier == "Silver" && item.IsEligibleForTierDiscount)
                price *= 0.97m;

            // Promotional codes
            foreach (var promo in order.PromoCodes)
            {
                if (promo.IsValid && promo.AppliesToCategory(item.Category))
                {
                    price *= 1 - promo.DiscountPercent;
                }
            }

            total += price * item.Quantity;
        }

        // Shipping
        var shipping = order.ShippingMethod switch
        {
            "Express" => 15.99m,
            "Standard" => 5.99m,
            "Free" => 0m,
            _ => 9.99m
        };

        // Tax
        var taxRate = order.State switch
        {
            "CA" => 0.0725m,
            "NY" => 0.08m,
            "TX" => 0.0625m,
            _ => 0.05m
        };

        total += shipping;
        total *= 1 + taxRate;

        return new ProcessingResult { Total = total, Status = "Processed" };
    }
}
```

### Compliant Code

```csharp
public class OrderProcessor
{
    private readonly IPricingService _pricingService;
    private readonly IShippingCalculator _shippingCalculator;
    private readonly ITaxCalculator _taxCalculator;

    public OrderProcessor(
        IPricingService pricingService,
        IShippingCalculator shippingCalculator,
        ITaxCalculator taxCalculator)
    {
        _pricingService = pricingService;
        _shippingCalculator = shippingCalculator;
        _taxCalculator = taxCalculator;
    }

    public ProcessingResult ProcessOrder(Order order)
    {
        if (order == null) return ProcessingResult.Invalid;
        if (!order.IsActive) return ProcessingResult.Inactive;

        var subtotal = _pricingService.CalculateSubtotal(order);
        var shipping = _shippingCalculator.Calculate(order);
        var tax = _taxCalculator.Calculate(subtotal + shipping, order.State);

        return new ProcessingResult
        {
            Total = subtotal + shipping + tax,
            Status = "Processed"
        };
    }
}

// Each service has focused responsibility and lower complexity
public class PricingService : IPricingService
{
    private readonly IVolumeDiscountCalculator _volumeDiscount;
    private readonly ICategoryDiscountCalculator _categoryDiscount;
    private readonly ICustomerTierDiscountCalculator _tierDiscount;
    private readonly IPromoCodeApplicator _promoApplicator;

    public decimal CalculateSubtotal(Order order)
    {
        return order.Items
            .Where(item => item.Quantity > 0)
            .Sum(item => CalculateItemPrice(item, order));
    }

    private decimal CalculateItemPrice(OrderItem item, Order order)
    {
        var price = item.UnitPrice;
        price = _volumeDiscount.Apply(price, item.Quantity);
        price = _categoryDiscount.Apply(price, item.Category);
        price = _tierDiscount.Apply(price, order.CustomerTier, item.IsEligibleForTierDiscount);
        price = _promoApplicator.Apply(price, order.PromoCodes, item.Category);
        return price * item.Quantity;
    }
}
```

## How to Fix

1. **Extract Methods**: Break down large methods into smaller, focused methods
2. **Use Strategy Pattern**: Replace complex switch/if-else chains with strategy objects
3. **Apply Dependency Injection**: Inject services for complex calculations
4. **Simplify Conditionals**: Use guard clauses and early returns
5. **Use LINQ**: Replace complex loops with declarative LINQ expressions

### Quick Fixes

```csharp
// Before: Complex nested conditionals
if (a && b && c || d && e)
{
    // ...
}

// After: Extract to descriptive method
if (ShouldProcess(a, b, c, d, e))
{
    // ...
}

private bool ShouldProcess(bool a, bool b, bool c, bool d, bool e)
{
    var conditionA = a && b && c;
    var conditionB = d && e;
    return conditionA || conditionB;
}
```

## Configuration

```ini
# .editorconfig

# Adjust severity
dotnet_diagnostic.SEAM020.severity = warning

# Set custom threshold (default: 25)
dotnet_code_quality.SEAM020.cyclomatic_complexity_threshold = 20
```

## When to Suppress

Suppression may be appropriate when:

- The method is **generated code** that cannot be modified
- The complexity is **inherent to the domain** (e.g., state machines, parsers)
- Refactoring would make the code **less readable** due to excessive indirection
- The code is **legacy** with a planned rewrite

```csharp
#pragma warning disable SEAM020
// This state machine maps directly to the specification
public State ProcessEvent(Event evt)
{
    // Complex but necessary state transition logic
}
#pragma warning restore SEAM020
```

## Related Rules

- [SEAM011](SEAM011.md) - Complex Private Method
- [CA1502](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1502) - Avoid excessive complexity (Microsoft rule)

## References

- [Cyclomatic Complexity](https://en.wikipedia.org/wiki/Cyclomatic_complexity) - Wikipedia
- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Clean Code](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882) by Robert C. Martin
