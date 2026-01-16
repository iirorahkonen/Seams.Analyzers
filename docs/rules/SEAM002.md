# SEAM002: Concrete Type in Constructor Parameter

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM002 |
| **Category** | DirectDependencies |
| **Severity** | Info |
| **Enabled** | No (opt-in) |

## Description

Detects constructor parameters that use concrete class types instead of interfaces or abstract classes. This reduces flexibility and makes testing more difficult.

## Why This Rule Is Disabled by Default

This rule is disabled by default because:

1. **High False Positive Rate**: Many concrete types are legitimately used (DTOs, value objects, configuration classes)
2. **Over-Abstraction Risk**: Not every dependency needs an interface
3. **Project-Specific Decisions**: What should be abstract varies by project architecture
4. **Gradual Adoption**: Enables incremental improvement without overwhelming noise

Enable this rule when you want to enforce strict interface-based dependency injection.

## Why This Is Problematic

When problematic patterns are detected:

1. **Cannot Mock**: Concrete classes require real implementations or mocking frameworks that use inheritance
2. **Tight Coupling**: Changes to the concrete class may break consuming classes
3. **Testing Complexity**: Mocking sealed or non-virtual methods requires special tools
4. **Hidden Implementations**: The specific implementation details leak into the consumer's API
5. **Liskov Substitution**: Cannot easily substitute different implementations

## Detection Logic

The analyzer flags constructor parameters when:
- The parameter type is a concrete class (not interface or abstract)
- The type name suggests a service (ends with Service, Repository, Provider, Factory, etc.)
- OR the type implements non-marker interfaces (IDisposable, IEquatable excluded)

## Examples

### Non-Compliant Code

```csharp
public class OrderProcessor
{
    // Bad: Concrete type UserRepository instead of IUserRepository
    public OrderProcessor(
        UserRepository userRepository,
        EmailService emailService,
        PaymentGateway paymentGateway)
    {
        // ...
    }
}
```

```csharp
public class NotificationService
{
    // Bad: Concrete SmtpClient
    public NotificationService(SmtpEmailClient emailClient)
    {
        // ...
    }
}
```

### Compliant Code

```csharp
public class OrderProcessor
{
    // Good: Interface-based dependencies
    public OrderProcessor(
        IUserRepository userRepository,
        IEmailService emailService,
        IPaymentGateway paymentGateway)
    {
        // ...
    }
}
```

```csharp
public class NotificationService
{
    // Good: Interface for email client
    public NotificationService(IEmailClient emailClient)
    {
        // ...
    }
}
```

```csharp
// OK: Concrete types that are acceptable
public class ReportGenerator
{
    public ReportGenerator(
        ReportOptions options,        // OK: Configuration/options class
        ILogger<ReportGenerator> logger,
        CancellationToken cancellationToken)  // OK: Framework type
    {
    }
}
```

## How to Fix

1. **Identify Interface**: Determine if an interface already exists for the concrete type
2. **Extract Interface**: If no interface exists, create one with the needed members
3. **Implement Interface**: Have the concrete class implement the interface
4. **Update Constructor**: Change parameter type from concrete to interface
5. **Update DI Registration**: Ensure the interface is registered in the container
6. **Update Tests**: Inject mocks via the interface

### Interface Extraction

```csharp
// Before: Concrete class
public class EmailService
{
    public void SendEmail(string to, string subject, string body) { }
    public void SendBulkEmail(IEnumerable<string> recipients, string subject, string body) { }
}

// After: Extract interface
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
    void SendBulkEmail(IEnumerable<string> recipients, string subject, string body);
}

public class EmailService : IEmailService
{
    // Implementation unchanged
}
```

## How to Enable

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM002.severity = suggestion

# Or as a warning
dotnet_diagnostic.SEAM002.severity = warning

# Exclude specific types that should remain concrete
dotnet_code_quality.SEAM002.excluded_types = DbContextOptions, ILogger
```

## When to Suppress

Suppression is appropriate when:

- The parameter is a **configuration/options class** (strongly-typed config)
- The type is a **framework type** that doesn't need abstraction (CancellationToken, ILogger)
- The concrete type is **sealed and well-tested** third-party code
- You're using **composition over inheritance** deliberately

```csharp
#pragma warning disable SEAM002
// This concrete type is an immutable configuration object
public OrderService(OrderConfiguration config)
#pragma warning restore SEAM002
{
}
```

## Configuration

```ini
# .editorconfig

# Enable and configure severity
dotnet_diagnostic.SEAM002.severity = suggestion

# Exclude specific types
dotnet_code_quality.SEAM002.excluded_types = MyConfigClass, SpecialHandler
```

## Related Rules

- [SEAM001](SEAM001.md) - Direct Instantiation
- [SEAM003](SEAM003.md) - Service Locator Pattern
- [SEAM009](SEAM009.md) - Sealed Classes

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Dependency Injection Principles, Practices, and Patterns](https://www.amazon.com/Dependency-Injection-Principles-Practices-Patterns/dp/161729473X) by Steven van Deursen and Mark Seemann
- [Interface Segregation Principle](https://en.wikipedia.org/wiki/Interface_segregation_principle)
