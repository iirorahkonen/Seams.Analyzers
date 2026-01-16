#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

namespace TestHarness.Analyzers.Samples.Patterns;

// SEAM001 - Direct Instantiation
// These examples show direct instantiation of concrete types that create hard dependencies

public class EmailService
{
    public void SendConfirmation(Order order) { }
}

public class FileLogger
{
    public void Log(string message) { }
}

public class Order
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; }
}

public class OrderProcessor
{
    public void ProcessOrder(Order order)
    {
        // SEAM001: Direct instantiation of EmailService creates a hard dependency
        var emailService = new EmailService();

        // SEAM001: Direct instantiation of FileLogger creates a hard dependency
        var logger = new FileLogger();

        logger.Log($"Processing order {order.Id}");
        emailService.SendConfirmation(order);
    }
}

// SEAM002 - Concrete Constructor Parameter
// These examples show constructor parameters that use concrete types instead of abstractions

public interface IPaymentGateway
{
    void ProcessPayment(decimal amount);
}

public class StripeClient : IPaymentGateway
{
    public void ProcessPayment(decimal amount) { }
}

// SEAM002: PaymentService depends on concrete StripeClient instead of IPaymentGateway
public class PaymentService
{
    private readonly StripeClient _client;

    // SEAM002: Should use IPaymentGateway instead of StripeClient
    public PaymentService(StripeClient client)
    {
        _client = client;
    }

    public void Pay(decimal amount) => _client.ProcessPayment(amount);
}

// SEAM003 - Service Locator Pattern
// These examples show usage of service locator anti-pattern

public interface IDataService
{
    object GetData();
}

public static class ServiceLocator
{
    public static T Resolve<T>() => default!;
}

public class ReportGenerator
{
    public void Generate()
    {
        // SEAM003: Service locator pattern hides dependencies
        var service = ServiceLocator.Resolve<IDataService>();
        var data = service.GetData();
    }
}
