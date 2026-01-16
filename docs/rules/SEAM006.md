# SEAM006: Guid.NewGuid Creates Non-Deterministic Dependency

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM006 |
| **Category** | StaticDependencies |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects usage of `Guid.NewGuid()` which creates non-deterministic values making testing difficult.

## Why This Is Problematic

Direct GUID generation causes testing challenges:

1. **Non-Deterministic Values**: Every test run produces different GUIDs
2. **Cannot Assert Specific Values**: Tests cannot verify exact GUID values without complex workarounds
3. **Snapshot Testing Fails**: GUID differences break snapshot/golden-file tests
4. **Correlation Difficulty**: Hard to trace entities through logs when IDs are random
5. **Test Data Setup**: Creating test data with known relationships requires GUID prediction
6. **Reproducibility**: Failed tests are harder to reproduce when IDs change

## Examples

### Non-Compliant Code

```csharp
public class OrderService
{
    public Order CreateOrder(OrderRequest request)
    {
        return new Order
        {
            // Bad: Direct Guid.NewGuid() usage
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new OrderItem
            {
                // Bad: Also here
                Id = Guid.NewGuid(),
                ProductId = i.ProductId
            }).ToList()
        };
    }
}
```

```csharp
public class DocumentService
{
    public Document Upload(Stream content, string fileName)
    {
        // Bad: Non-deterministic ID generation
        var documentId = Guid.NewGuid();

        SaveToStorage(documentId.ToString(), content);

        return new Document
        {
            Id = documentId,
            FileName = fileName,
            UploadedAt = DateTime.UtcNow
        };
    }
}
```

### Compliant Code

Using a dedicated interface:

```csharp
public interface IGuidGenerator
{
    Guid NewGuid();
}

public class GuidGenerator : IGuidGenerator
{
    public Guid NewGuid() => Guid.NewGuid();
}

public class OrderService
{
    private readonly IGuidGenerator _guidGenerator;

    public OrderService(IGuidGenerator guidGenerator)
    {
        _guidGenerator = guidGenerator;
    }

    public Order CreateOrder(OrderRequest request)
    {
        return new Order
        {
            Id = _guidGenerator.NewGuid(),
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new OrderItem
            {
                Id = _guidGenerator.NewGuid(),
                ProductId = i.ProductId
            }).ToList()
        };
    }
}
```

Test implementation:

```csharp
// Sequential GUID generator for predictable tests
public class SequentialGuidGenerator : IGuidGenerator
{
    private int _counter;

    public Guid NewGuid()
    {
        _counter++;
        return new Guid($"00000000-0000-0000-0000-{_counter:D12}");
    }
}

// Or a fixed list of GUIDs
public class FixedGuidGenerator : IGuidGenerator
{
    private readonly Queue<Guid> _guids;

    public FixedGuidGenerator(params Guid[] guids)
    {
        _guids = new Queue<Guid>(guids);
    }

    public Guid NewGuid() =>
        _guids.Count > 0 ? _guids.Dequeue() : Guid.NewGuid();
}
```

Using factory pattern:

```csharp
public class DocumentService
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IDocumentStorage _storage;

    public DocumentService(IGuidGenerator guidGenerator, IDocumentStorage storage)
    {
        _guidGenerator = guidGenerator;
        _storage = storage;
    }

    public Document Upload(Stream content, string fileName)
    {
        var documentId = _guidGenerator.NewGuid();

        _storage.Save(documentId.ToString(), content);

        return new Document
        {
            Id = documentId,
            FileName = fileName
        };
    }
}
```

## How to Fix

1. **Create Interface**: Define `IGuidGenerator` with a `NewGuid()` method
2. **Create Implementation**: Implement with `Guid.NewGuid()` for production
3. **Inject Dependency**: Add the generator as a constructor parameter
4. **Register in DI**: Add `IGuidGenerator` to your container as singleton
5. **Create Test Doubles**: Implement predictable generators for testing

### DI Registration

```csharp
// In Program.cs / Startup.cs
services.AddSingleton<IGuidGenerator, GuidGenerator>();
```

### Alternative: Using Func<Guid>

For simpler cases:

```csharp
public class EntityFactory
{
    private readonly Func<Guid> _generateId;

    public EntityFactory(Func<Guid>? generateId = null)
    {
        _generateId = generateId ?? Guid.NewGuid;
    }

    public Entity Create(string name) => new()
    {
        Id = _generateId(),
        Name = name
    };
}

// Test usage
var knownId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var factory = new EntityFactory(() => knownId);
```

## When to Suppress

Suppression is appropriate when:

- GUIDs are used for **logging correlation IDs** where exact values don't matter in tests
- The code is in a **database migration** or one-time script
- You're generating **temporary file names** where uniqueness is all that matters
- Tests use **integration testing** against real databases that generate IDs

```csharp
#pragma warning disable SEAM006
// Correlation ID for logging - test assertions don't need exact value
var correlationId = Guid.NewGuid();
_logger.LogInformation("Starting operation {CorrelationId}", correlationId);
#pragma warning restore SEAM006
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM006.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM006.severity = suggestion
```

## Related Rules

- [SEAM005](SEAM005.md) - DateTime.Now/UtcNow (similar non-determinism)
- [SEAM004](SEAM004.md) - Static Method Calls (general pattern)
- [SEAM008](SEAM008.md) - Static Property Access (related pattern)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Test Doubles](https://martinfowler.com/bliki/TestDouble.html) by Martin Fowler
- [Deterministic Testing](https://www.youtube.com/watch?v=PlfouAzF-ao) - NDC Conference
