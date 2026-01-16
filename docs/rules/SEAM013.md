# SEAM013: Static Mutable Field Creates Shared State

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM013 |
| **Category** | GlobalState |
| **Severity** | Warning |
| **Enabled** | Yes |

## Description

Detects static mutable fields that create shared state persisting across tests. Static `readonly` and `const` fields are excluded as they are immutable.

## Why This Is Problematic

Static mutable fields cause serious testability problems:

1. **Test Pollution**: State changes in one test affect subsequent tests
2. **Flaky Tests**: Tests may pass or fail depending on execution order
3. **Parallel Test Failures**: Concurrent tests interfere with each other through shared state
4. **Hidden State**: The field's existence isn't visible to code consuming the class
5. **Difficult Reset**: Resetting state between tests requires reflection or test-specific hooks
6. **Memory Leaks**: Static references prevent garbage collection for the lifetime of the application

## Examples

### Non-Compliant Code

```csharp
public class CacheManager
{
    // Bad: Static mutable dictionary
    private static Dictionary<string, object> _cache = new();

    // Bad: Static mutable list
    private static List<string> _processedItems = new();

    // Bad: Static delegate (can be reassigned)
    public static Action<string> OnItemCached;

    // Bad: Static mutable reference type
    private static CacheConfiguration _config;

    public void CacheItem(string key, object value)
    {
        _cache[key] = value;
        _processedItems.Add(key);
        OnItemCached?.Invoke(key);
    }
}
```

```csharp
public class MetricsCollector
{
    // Bad: Static counters
    private static int _requestCount;
    private static int _errorCount;

    public void RecordRequest() => _requestCount++;
    public void RecordError() => _errorCount++;

    public static int TotalRequests => _requestCount;
    public static int TotalErrors => _errorCount;
}
```

### Compliant Code

```csharp
public class CacheManager : ICacheManager
{
    // Good: Instance field instead of static
    private readonly Dictionary<string, object> _cache = new();
    private readonly List<string> _processedItems = new();

    // Good: Event on instance
    public event Action<string>? OnItemCached;

    private readonly CacheConfiguration _config;

    public CacheManager(CacheConfiguration config)
    {
        _config = config;
    }

    public void CacheItem(string key, object value)
    {
        _cache[key] = value;
        _processedItems.Add(key);
        OnItemCached?.Invoke(key);
    }
}

// Register as singleton if shared state is needed
services.AddSingleton<ICacheManager, CacheManager>();
```

```csharp
public class MetricsCollector : IMetricsCollector
{
    // Good: Instance fields
    private int _requestCount;
    private int _errorCount;

    public void RecordRequest() => Interlocked.Increment(ref _requestCount);
    public void RecordError() => Interlocked.Increment(ref _errorCount);

    public int TotalRequests => _requestCount;
    public int TotalErrors => _errorCount;

    // Good: Method to reset for testing
    public void Reset()
    {
        _requestCount = 0;
        _errorCount = 0;
    }
}
```

```csharp
// OK: Static readonly immutable types
public class Constants
{
    // OK: readonly reference to immutable collection
    private static readonly ImmutableArray<string> _validCodes =
        ImmutableArray.Create("A", "B", "C");

    // OK: const values
    private const int MaxRetries = 3;

    // OK: static readonly string (strings are immutable)
    private static readonly string DefaultName = "Default";
}
```

## How to Fix

1. **Convert to Instance Fields**: Change static fields to instance fields
2. **Use DI for Shared State**: If shared state is needed, register the class as a singleton in DI
3. **Extract Interface**: Create an interface to enable mocking
4. **Add Reset Capability**: For testing scenarios, add a method to reset state
5. **Consider Immutable Alternatives**: Use `ImmutableArray`, `ImmutableDictionary`, etc.

### Migration Example

```csharp
// Before: Static mutable state
public class FeatureFlags
{
    private static Dictionary<string, bool> _flags = new();

    public static void SetFlag(string name, bool value) => _flags[name] = value;
    public static bool IsEnabled(string name) => _flags.TryGetValue(name, out var v) && v;
}

// After: Testable instance-based design
public interface IFeatureFlags
{
    void SetFlag(string name, bool value);
    bool IsEnabled(string name);
}

public class FeatureFlags : IFeatureFlags
{
    private readonly Dictionary<string, bool> _flags = new();

    public void SetFlag(string name, bool value) => _flags[name] = value;
    public bool IsEnabled(string name) => _flags.TryGetValue(name, out var v) && v;
}

// Register as singleton to maintain shared state behavior
services.AddSingleton<IFeatureFlags, FeatureFlags>();
```

## When to Suppress

Suppression is appropriate when:

- The field is used for **thread-safe caching** with proper synchronization
- You're implementing **logging infrastructure** that must be globally accessible
- The state is **intentionally shared** across the entire application and tests don't need isolation
- You're in **test code** where static state is acceptable (test fixtures)

```csharp
#pragma warning disable SEAM013
// Intentionally shared logging output for debugging
private static List<string> _debugLog = new();
#pragma warning restore SEAM013
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM013.severity = none

# Or set to suggestion instead of warning
dotnet_diagnostic.SEAM013.severity = suggestion
```

## Related Rules

- [SEAM012](SEAM012.md) - Singleton Pattern (often uses static mutable fields)
- [SEAM014](SEAM014.md) - Ambient Context (another global state pattern)
- [SEAM008](SEAM008.md) - Static Property Access (related static dependency)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Global State and Singletons](https://testing.googleblog.com/2008/11/clean-code-talks-global-state-and.html) - Google Testing Blog
- [Thread Safety and Shared State](https://docs.microsoft.com/en-us/dotnet/standard/threading/managed-threading-best-practices)
