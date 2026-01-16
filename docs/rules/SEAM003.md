# SEAM003: Service Locator Pattern Usage

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM003 |
| **Category** | DirectDependencies |
| **Severity** | Warning |
| **Enabled** | Yes |

## Description

Detects usage of the Service Locator anti-pattern, which hides dependencies and makes code harder to test.

## Why This Is Problematic

The Service Locator pattern is considered an anti-pattern for several reasons:

1. **Hidden Dependencies**: Dependencies are not visible in the class constructor, making it unclear what a class needs to function
2. **Difficult to Test**: You must configure a service locator in tests, which adds complexity and often leads to shared state between tests
3. **Runtime Errors**: Missing dependencies are only discovered at runtime when `Resolve()` is called, not at compile time
4. **Couples Code to Container**: Your code becomes dependent on a specific DI container implementation
5. **Violates Explicit Dependencies Principle**: Classes should clearly declare their dependencies through their constructors

## Examples

### Non-Compliant Code

```csharp
public class OrderProcessor
{
    public void ProcessOrder(Order order)
    {
        // Bad: Resolving dependencies from a service locator
        var emailService = ServiceLocator.Resolve<IEmailService>();
        var paymentGateway = ServiceLocator.GetService<IPaymentGateway>();
        var logger = DependencyResolver.Current.GetService<ILogger>();

        // Process the order...
        paymentGateway.Charge(order.Total);
        emailService.SendConfirmation(order.Customer.Email);
    }
}
```

```csharp
public class UserService
{
    private readonly IServiceProvider _serviceProvider;

    public UserService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void CreateUser(User user)
    {
        // Bad: Using IServiceProvider outside composition root
        var repository = _serviceProvider.GetService<IUserRepository>();
        var validator = _serviceProvider.GetRequiredService<IUserValidator>();

        validator.Validate(user);
        repository.Save(user);
    }
}
```

### Compliant Code

```csharp
public class OrderProcessor
{
    private readonly IEmailService _emailService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<OrderProcessor> _logger;

    // Good: Dependencies are explicitly declared in constructor
    public OrderProcessor(
        IEmailService emailService,
        IPaymentGateway paymentGateway,
        ILogger<OrderProcessor> logger)
    {
        _emailService = emailService;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public void ProcessOrder(Order order)
    {
        _paymentGateway.Charge(order.Total);
        _emailService.SendConfirmation(order.Customer.Email);
    }
}
```

```csharp
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly IUserValidator _validator;

    // Good: Dependencies injected via constructor
    public UserService(IUserRepository repository, IUserValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public void CreateUser(User user)
    {
        _validator.Validate(user);
        _repository.Save(user);
    }
}
```

## How to Fix

1. **Identify Hidden Dependencies**: Find all calls to `Resolve()`, `GetService()`, `GetInstance()`, etc.
2. **Add Constructor Parameters**: Add the required dependencies as constructor parameters
3. **Store in Private Fields**: Assign injected dependencies to private readonly fields
4. **Register in DI Container**: Ensure all dependencies are registered in your composition root (Startup.cs, Program.cs)
5. **Update Tests**: Inject mock dependencies directly in tests

## When to Suppress

Suppression is appropriate when:

- You're in the **composition root** (Startup.cs, Program.cs, ConfigureServices method)
- You're implementing a **factory pattern** that requires runtime resolution (e.g., `Func<IService>` or factory delegates)
- You're working with **plugin architectures** where types are discovered at runtime
- You're in a **migration period** gradually moving from Service Locator to Constructor Injection

```csharp
// Suppression example for legitimate factory use
#pragma warning disable SEAM003
services.AddScoped<IOrderProcessor>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return config["UseNewProcessor"] == "true"
        ? sp.GetRequiredService<NewOrderProcessor>()
        : sp.GetRequiredService<LegacyOrderProcessor>();
});
#pragma warning restore SEAM003
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM003.severity = none

# Or set to suggestion instead of warning
dotnet_diagnostic.SEAM003.severity = suggestion
```

## Related Rules

- [SEAM001](SEAM001.md) - Direct Instantiation (another form of hidden dependencies)
- [SEAM002](SEAM002.md) - Concrete Constructor Parameters (prefer interfaces)
- [SEAM014](SEAM014.md) - Ambient Context (similar hidden dependency pattern)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Service Locator is an Anti-Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) by Mark Seemann
- [Dependency Injection Principles, Practices, and Patterns](https://www.amazon.com/Dependency-Injection-Principles-Practices-Patterns/dp/161729473X) by Steven van Deursen and Mark Seemann
