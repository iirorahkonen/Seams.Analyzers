# SEAM001: Direct Instantiation of Concrete Type

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM001 |
| **Category** | DirectDependencies |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects direct instantiation of concrete types using the `new` keyword that creates hard dependencies preventing seam injection for testing.

## Why This Is Problematic

Direct instantiation creates tight coupling that harms testability:

1. **Hard Dependencies**: The class is tightly coupled to the specific implementation
2. **No Substitution**: Cannot replace the dependency with a test double (mock, stub, fake)
3. **Hidden Dependencies**: Dependencies created inside methods aren't visible in the class API
4. **Cascading Dependencies**: The instantiated type may have its own dependencies, creating a dependency chain
5. **Difficult to Test**: Unit tests must use the real implementation or resort to hacks

## What Gets Flagged

The analyzer flags `new` expressions for:
- External library types (not defined in the same project)
- Types with methods (not pure data classes/DTOs)

The analyzer **skips**:
- Value types and primitives
- Collection types (List, Dictionary, etc.)
- Exception types, StringBuilder, Uri
- Data classes with only properties (DTOs, records)
- Types defined in the same project
- Field initializers

## Examples

### Non-Compliant Code

```csharp
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        // Bad: Direct instantiation of email service
        var emailService = new SmtpEmailService();
        emailService.SendConfirmation(order.Customer.Email);

        // Bad: Direct instantiation of external logging
        var logger = new FileLogger("orders.log");
        logger.Log($"Order {order.Id} processed");
    }
}
```

```csharp
public class ReportGenerator
{
    public Report GenerateReport(ReportRequest request)
    {
        // Bad: Direct instantiation of PDF library
        var pdfWriter = new PdfSharpWriter();

        // Bad: Direct instantiation of data access
        var repository = new SqlOrderRepository();

        var data = repository.GetOrderData(request.StartDate, request.EndDate);
        return pdfWriter.CreateReport(data);
    }
}
```

### Compliant Code

```csharp
public class OrderService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderService> _logger;

    // Good: Dependencies injected via constructor
    public OrderService(IEmailService emailService, ILogger<OrderService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public void ProcessOrder(Order order)
    {
        _emailService.SendConfirmation(order.Customer.Email);
        _logger.LogInformation("Order {OrderId} processed", order.Id);
    }
}
```

```csharp
public class ReportGenerator
{
    private readonly IPdfWriter _pdfWriter;
    private readonly IOrderRepository _repository;

    // Good: Dependencies injected
    public ReportGenerator(IPdfWriter pdfWriter, IOrderRepository repository)
    {
        _pdfWriter = pdfWriter;
        _repository = repository;
    }

    public Report GenerateReport(ReportRequest request)
    {
        var data = _repository.GetOrderData(request.StartDate, request.EndDate);
        return _pdfWriter.CreateReport(data);
    }
}
```

```csharp
// OK: Instantiating DTOs and value objects
public class OrderFactory
{
    public Order CreateOrder(OrderRequest request)
    {
        // OK: Order is a data class
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
```

## How to Fix

1. **Identify the Dependency**: Determine what capability the instantiated type provides
2. **Extract Interface**: Create an interface representing that capability
3. **Implement the Interface**: Have the concrete type implement the interface
4. **Add Constructor Parameter**: Add the interface as a constructor parameter
5. **Register in DI**: Add the implementation to your DI container
6. **Create Test Doubles**: Implement mocks/fakes for testing

### Using Factory Pattern

For cases where object creation is part of the class's responsibility:

```csharp
// Define factory interface
public interface INotificationFactory
{
    INotification Create(NotificationType type);
}

// Implement factory
public class NotificationFactory : INotificationFactory
{
    public INotification Create(NotificationType type) => type switch
    {
        NotificationType.Email => new EmailNotification(),
        NotificationType.Sms => new SmsNotification(),
        _ => throw new ArgumentException($"Unknown type: {type}")
    };
}

// Use factory
public class NotificationService
{
    private readonly INotificationFactory _factory;

    public NotificationService(INotificationFactory factory)
    {
        _factory = factory;
    }

    public void Send(NotificationType type, string message)
    {
        var notification = _factory.Create(type);
        notification.Send(message);
    }
}
```

## When to Suppress

Suppression is appropriate when:

- Instantiating **pure data objects** not detected by the analyzer's heuristics
- Creating **builder objects** (StringBuilder, UriBuilder)
- Working with **framework requirements** that mandate direct instantiation
- In **factory classes** where object creation is the explicit purpose

```csharp
#pragma warning disable SEAM001
// This is a factory method - object creation is intentional
var client = new SpecializedHttpClient(options);
#pragma warning restore SEAM001
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM001.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM001.severity = suggestion

# Exclude specific types
dotnet_code_quality.SEAM001.excluded_types = MyLegacyService, ThirdPartyClient
```

## Related Rules

- [SEAM002](SEAM002.md) - Concrete Constructor Parameters
- [SEAM003](SEAM003.md) - Service Locator Pattern
- [SEAM016](SEAM016.md) - HttpClient Creation (specific case)
- [SEAM017](SEAM017.md) - Database Access (specific case)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Dependency Injection Principles, Practices, and Patterns](https://www.amazon.com/Dependency-Injection-Principles-Practices-Patterns/dp/161729473X) by Steven van Deursen and Mark Seemann
- [Seams in Software](https://www.informit.com/articles/article.aspx?p=359417) - Michael Feathers
