# SEAM019: Random.Shared Creates Non-Deterministic Dependency

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM019 |
| **Category** | StaticDependencies |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects usage of `Random.Shared` which creates non-deterministic values making testing difficult. `Random.Shared` was introduced in .NET 6 as a thread-safe shared Random instance.

## Why This Is Problematic

Direct usage of `Random.Shared` causes testing challenges:

1. **Non-Deterministic Values**: Every test run produces different random numbers
2. **Cannot Assert Specific Values**: Tests cannot verify exact values without complex workarounds
3. **Flaky Tests**: Randomness can cause tests to pass or fail unpredictably
4. **Edge Case Testing**: Hard to test boundary conditions that depend on specific random values
5. **Reproducibility**: Failed tests are harder to reproduce when values change
6. **Snapshot Testing Fails**: Random value differences break snapshot/golden-file tests

## Examples

### Non-Compliant Code

```csharp
public class GameService
{
    public int RollDice()
    {
        // Bad: Direct Random.Shared usage
        return Random.Shared.Next(1, 7);
    }
}
```

```csharp
public class PasswordGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public string Generate(int length)
    {
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            // Bad: Non-deterministic character selection
            result[i] = Chars[Random.Shared.Next(Chars.Length)];
        }
        return new string(result);
    }
}
```

```csharp
public class LoadBalancer
{
    private readonly List<Server> _servers;

    public Server SelectServer()
    {
        // Bad: Random server selection is untestable
        var index = Random.Shared.Next(_servers.Count);
        return _servers[index];
    }
}
```

### Compliant Code

Inject a Random instance:

```csharp
public class GameService
{
    private readonly Random _random;

    public GameService(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public int RollDice()
    {
        return _random.Next(1, 7);
    }
}

// Test usage with seeded random for predictable results
[Fact]
public void RollDice_WithSeededRandom_ReturnsPredictableValue()
{
    var seededRandom = new Random(42); // Deterministic seed
    var service = new GameService(seededRandom);

    var result = service.RollDice();

    Assert.Equal(4, result); // Known value for seed 42
}
```

Using an interface for more control:

```csharp
public interface IRandomGenerator
{
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
}

public class RandomGenerator : IRandomGenerator
{
    private readonly Random _random = Random.Shared;

    public int Next(int maxValue) => _random.Next(maxValue);
    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    public double NextDouble() => _random.NextDouble();
}

public class LoadBalancer
{
    private readonly List<Server> _servers;
    private readonly IRandomGenerator _random;

    public LoadBalancer(List<Server> servers, IRandomGenerator random)
    {
        _servers = servers;
        _random = random;
    }

    public Server SelectServer()
    {
        var index = _random.Next(_servers.Count);
        return _servers[index];
    }
}
```

Test implementation:

```csharp
public class FixedRandomGenerator : IRandomGenerator
{
    private readonly Queue<int> _intValues;
    private readonly Queue<double> _doubleValues;

    public FixedRandomGenerator(params int[] values)
    {
        _intValues = new Queue<int>(values);
        _doubleValues = new Queue<double>();
    }

    public int Next(int maxValue) => _intValues.Dequeue();
    public int Next(int minValue, int maxValue) => _intValues.Dequeue();
    public double NextDouble() => _doubleValues.Dequeue();
}

[Fact]
public void SelectServer_ReturnsCorrectServer()
{
    var servers = new List<Server> { serverA, serverB, serverC };
    var random = new FixedRandomGenerator(1); // Always returns index 1
    var balancer = new LoadBalancer(servers, random);

    var result = balancer.SelectServer();

    Assert.Same(serverB, result);
}
```

## How to Fix

1. **Inject Random**: Accept a `Random` instance as constructor parameter
2. **Default to Shared**: Use `Random.Shared` as default when not testing
3. **Use Seeded Random**: Create `new Random(seed)` in tests for determinism
4. **Or Create Interface**: Define `IRandomGenerator` for more complex scenarios
5. **Register in DI**: Add the abstraction to your container

### DI Registration

```csharp
// In Program.cs / Startup.cs
services.AddSingleton<IRandomGenerator, RandomGenerator>();

// Or simply register a seeded Random for testing environments
services.AddSingleton(new Random(42));
```

## When to Suppress

Suppression is appropriate when:

- Random values are used for **non-critical decisions** where exact values don't matter in tests
- The code is in a **one-time script** or utility
- You're generating **temporary identifiers** where uniqueness is all that matters
- Tests use **statistical assertions** (e.g., "value should be between X and Y")

```csharp
#pragma warning disable SEAM019
// Shuffle order doesn't matter for test assertions
var shuffledItems = items.OrderBy(_ => Random.Shared.Next()).ToList();
#pragma warning restore SEAM019
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM019.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM019.severity = suggestion
```

## Related Rules

- [SEAM006](SEAM006.md) - Guid.NewGuid (similar non-determinism)
- [SEAM005](SEAM005.md) - DateTime.Now/UtcNow (similar non-determinism)
- [SEAM004](SEAM004.md) - Static Method Calls (general pattern)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Random.Shared Property](https://learn.microsoft.com/en-us/dotnet/api/system.random.shared) - Microsoft Learn
- [Test Doubles](https://martinfowler.com/bliki/TestDouble.html) by Martin Fowler
