#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

namespace Seams.Analyzers.Samples.Patterns;

// SEAM009 - Sealed Class
// These examples show sealed classes that prevent inheritance-based testing

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// SEAM009: Sealed class prevents subclassing for testing
public sealed class UserRepository
{
    public User? GetById(int id) => null;
    public void Save(User user) { }
    public void Delete(int id) { }
}

// SEAM010 - Non-Virtual Method
// These examples show non-virtual methods that prevent override-based testing

public class OrderService
{
    // SEAM010: Non-virtual method cannot be overridden for testing
    public void CreateOrder(Order order)
    {
        ValidateOrder(order);
        SaveOrder(order);
        NotifyCustomer(order);
    }

    // SEAM010: Non-virtual method
    public void CancelOrder(int orderId)
    {
        // Cancellation logic
    }

    protected virtual void ValidateOrder(Order order) { }
    protected virtual void SaveOrder(Order order) { }
    protected virtual void NotifyCustomer(Order order) { }
}

// SEAM011 - Complex Private Method
// These examples show complex private methods that should be extracted

public class Invoice
{
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public List<InvoiceItem> Items { get; set; } = new();
}

public class InvoiceItem
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
}

public class InvoiceCalculator
{
    public decimal Calculate(Invoice invoice)
    {
        return CalculateWithTax(invoice);
    }

    // SEAM011: Complex private method (if > 50 lines) should be extracted
    // This example is simplified, but in real code this might be 100+ lines
    private decimal CalculateWithTax(Invoice invoice)
    {
        decimal subtotal = 0;

        foreach (var item in invoice.Items)
        {
            var itemTotal = item.Price * item.Quantity;
            var discount = itemTotal * item.Discount;
            subtotal += itemTotal - discount;
        }

        var tax = subtotal * invoice.TaxRate;
        var total = subtotal + tax;

        // ... imagine 50+ more lines of complex logic here ...
        // - Promotional discounts
        // - Loyalty points
        // - Currency conversion
        // - Rounding rules
        // - Regional tax calculations
        // - Shipping costs
        // etc.

        return total;
    }
}
