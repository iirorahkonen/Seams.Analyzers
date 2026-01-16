# SEAM010: Non-Virtual Method Prevents Override Seam

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM010 |
| **Category** | InheritanceBlockers |
| **Severity** | Info |
| **Enabled** | No (opt-in) |

## Description

Detects non-virtual public methods in public, non-sealed classes that cannot be overridden for testing purposes. The "subclass and override" technique requires methods to be virtual.

## Why This Rule Is Disabled by Default

This rule is disabled by default because:

1. **High Volume**: Most methods in C# are non-virtual by default
2. **Interface Preference**: Using interfaces is usually preferred over inheritance
3. **Performance**: Non-virtual calls are faster than virtual calls
4. **Design Intent**: Many methods shouldn't be overridden
5. **Modern Mocking**: Interface mocking doesn't require virtual methods

Enable this rule when you need to use subclass-and-override techniques for legacy code.

## Detection Logic

The analyzer flags methods when:
- The method is `public` in a `public`, non-sealed class
- The method is NOT `virtual`, `override`, or `abstract`
- The method is NOT `static`
- The method is NOT an interface implementation
- The method is NOT a simple expression-bodied or single-return method
- The method is NOT `Dispose`, `ToString`, `GetHashCode`, or similar common patterns

## Why This Is Problematic

In legacy code testing scenarios:

1. **Cannot Override**: Test subclasses cannot provide alternative implementations
2. **No Sensing Points**: Cannot intercept calls to verify behavior
3. **Dependency Breaking**: Cannot stub out problematic dependencies in methods
4. **Slow Tests**: Must use real implementations or complex mocking frameworks

However, modern practices often favor:
- **Interfaces** for abstraction points
- **Composition** over inheritance
- **Non-virtual by default** for performance and safety

## Examples

### Non-Compliant Code

```csharp
public class PaymentProcessor
{
    // Bad: Non-virtual prevents override in test subclass
    public bool ProcessPayment(PaymentRequest request)
    {
        if (!ValidateRequest(request))
            return false;

        var result = ChargeCard(request);
        SendConfirmation(request, result);
        return result.Success;
    }

    // Bad: Cannot stub this in tests
    public PaymentResult ChargeCard(PaymentRequest request)
    {
        // Calls external payment gateway
        return _gateway.Charge(request);
    }

    // Bad: Cannot prevent emails in tests
    public void SendConfirmation(PaymentRequest request, PaymentResult result)
    {
        _emailService.Send(request.Email, "Payment processed");
    }
}
```

### Compliant Code

Using interfaces (preferred):

```csharp
public interface IPaymentProcessor
{
    bool ProcessPayment(PaymentRequest request);
}

public interface IPaymentGateway
{
    PaymentResult Charge(PaymentRequest request);
}

public interface IEmailService
{
    void Send(string email, string message);
}

public class PaymentProcessor : IPaymentProcessor
{
    private readonly IPaymentGateway _gateway;
    private readonly IEmailService _emailService;

    public PaymentProcessor(IPaymentGateway gateway, IEmailService emailService)
    {
        _gateway = gateway;
        _emailService = emailService;
    }

    public bool ProcessPayment(PaymentRequest request)
    {
        if (!ValidateRequest(request))
            return false;

        var result = _gateway.Charge(request);
        _emailService.Send(request.Email, "Payment processed");
        return result.Success;
    }
}
```

Using virtual methods for legacy code:

```csharp
public class PaymentProcessor
{
    // Good: Virtual allows override in test subclass
    public virtual bool ProcessPayment(PaymentRequest request)
    {
        if (!ValidateRequest(request))
            return false;

        var result = ChargeCard(request);
        SendConfirmation(request, result);
        return result.Success;
    }

    // Good: Can be stubbed in tests
    public virtual PaymentResult ChargeCard(PaymentRequest request)
    {
        return _gateway.Charge(request);
    }

    // Good: Can be overridden to prevent emails
    public virtual void SendConfirmation(PaymentRequest request, PaymentResult result)
    {
        _emailService.Send(request.Email, "Payment processed");
    }
}

// Test subclass
public class TestablePaymentProcessor : PaymentProcessor
{
    public PaymentResult FakeResult { get; set; }
    public bool ConfirmationSent { get; private set; }

    public override PaymentResult ChargeCard(PaymentRequest request)
    {
        return FakeResult; // Return controlled result
    }

    public override void SendConfirmation(PaymentRequest request, PaymentResult result)
    {
        ConfirmationSent = true; // Track call without sending email
    }
}
```

## How to Fix

Consider your testing strategy:

1. **Prefer Interfaces**: Extract dependencies to interfaces and inject them
2. **Add Virtual**: Make methods virtual to enable subclass-and-override
3. **Extract Classes**: Move behavior to separate classes that can be mocked
4. **Evaluate Necessity**: Not every method needs to be testable in isolation

### When to Add Virtual

```csharp
// Before: Coupled to external systems
public class ReportService
{
    public void GenerateReport(int customerId)
    {
        var data = FetchData(customerId);   // Calls database
        var pdf = CreatePdf(data);          // CPU-bound
        SendEmail(pdf);                      // Calls SMTP
    }

    public CustomerData FetchData(int id) { /* ... */ }
    public byte[] CreatePdf(CustomerData data) { /* ... */ }
    public void SendEmail(byte[] attachment) { /* ... */ }
}

// After: Virtual enables selective override
public class ReportService
{
    public void GenerateReport(int customerId)
    {
        var data = FetchData(customerId);
        var pdf = CreatePdf(data);
        SendEmail(pdf);
    }

    public virtual CustomerData FetchData(int id) { /* ... */ }
    public byte[] CreatePdf(CustomerData data) { /* ... */ } // Pure, no need for virtual
    public virtual void SendEmail(byte[] attachment) { /* ... */ }
}
```

## How to Enable

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM010.severity = suggestion

# Exclude specific methods
dotnet_code_quality.SEAM010.excluded_methods = Dispose, Initialize
```

## When to Suppress

Suppression is appropriate when:

- The class **implements interfaces** used for mocking
- The method is **intentionally non-virtual** for design reasons
- The method is a **simple accessor** or expression-bodied method
- You have **alternative testing strategies**

```csharp
#pragma warning disable SEAM010
// Interface IPaymentProcessor is used for mocking
public bool ProcessPayment(PaymentRequest request)
{
}
#pragma warning restore SEAM010
```

## Configuration

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM010.severity = suggestion

# Exclude specific methods
dotnet_code_quality.SEAM010.excluded_methods = Initialize, Configure
```

## Related Rules

- [SEAM009](SEAM009.md) - Sealed Classes
- [SEAM011](SEAM011.md) - Complex Private Methods
- [SEAM002](SEAM002.md) - Concrete Constructor Parameters

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers - "Subclass and Override Method" technique
- [Virtual Members](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/virtual)
- [Dependency Breaking Techniques](https://www.informit.com/articles/article.aspx?p=359417&seqNum=3)
