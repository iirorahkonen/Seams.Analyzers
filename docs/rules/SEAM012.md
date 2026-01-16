# SEAM012: Singleton Pattern Creates Global State

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM012 |
| **Category** | GlobalState |
| **Severity** | Warning |
| **Enabled** | Yes |

## Description

Detects the classic Singleton pattern implementation (private constructor + static Instance property) which creates global state that persists across tests.

## Why This Is Problematic

The Singleton pattern creates several testability issues:

1. **Global State**: The single instance persists across test runs, causing test pollution
2. **Hidden Dependencies**: Classes using the singleton don't declare it as a dependency
3. **Difficult to Mock**: You cannot easily substitute a test double for the singleton
4. **Parallel Test Issues**: State shared between parallel tests causes race conditions and flaky tests
5. **Tight Coupling**: Code becomes tightly coupled to the specific singleton implementation
6. **Hard to Reset**: No clean way to reset state between tests without reflection hacks

## Examples

### Non-Compliant Code

```csharp
// Bad: Classic singleton pattern
public sealed class ConfigurationManager
{
    private static ConfigurationManager? _instance;
    private static readonly object _lock = new();

    private ConfigurationManager()
    {
        // Load configuration...
    }

    public static ConfigurationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ConfigurationManager();
                }
            }
            return _instance;
        }
    }

    public string GetSetting(string key) => /* ... */;
}

// Usage (hidden dependency)
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        var timeout = ConfigurationManager.Instance.GetSetting("OrderTimeout");
        // ...
    }
}
```

```csharp
// Bad: Lazy singleton variant
public class Logger
{
    private static readonly Lazy<Logger> _instance = new(() => new Logger());

    private Logger() { }

    public static Logger Current => _instance.Value;

    public void Log(string message) { /* ... */ }
}
```

### Compliant Code

```csharp
// Good: Use DI container to manage lifetime
public class ConfigurationManager : IConfigurationManager
{
    public ConfigurationManager()
    {
        // Load configuration...
    }

    public string GetSetting(string key) => /* ... */;
}

// Register as singleton in DI container
services.AddSingleton<IConfigurationManager, ConfigurationManager>();

// Usage with explicit dependency
public class OrderService
{
    private readonly IConfigurationManager _config;

    public OrderService(IConfigurationManager config)
    {
        _config = config;
    }

    public void ProcessOrder(Order order)
    {
        var timeout = _config.GetSetting("OrderTimeout");
        // ...
    }
}
```

```csharp
// Good: Testable logging
public interface ILogger
{
    void Log(string message);
}

public class Logger : ILogger
{
    public void Log(string message) { /* ... */ }
}

// Register in DI
services.AddSingleton<ILogger, Logger>();
```

## How to Fix

1. **Extract Interface**: Create an interface for the singleton class
2. **Make Constructor Public**: Remove the private constructor constraint
3. **Remove Static Instance**: Delete the static Instance/Current property
4. **Register in DI Container**: Use `AddSingleton<T>()` to maintain single-instance behavior
5. **Inject via Constructor**: Add the interface as a constructor parameter to consuming classes
6. **Update Tests**: Inject mock implementations in tests

### Migration Strategy

```csharp
// Step 1: Add interface (keep singleton temporarily for backwards compatibility)
public interface IConfigurationManager
{
    string GetSetting(string key);
}

public sealed class ConfigurationManager : IConfigurationManager
{
    // Keep existing singleton code temporarily
    [Obsolete("Use dependency injection instead")]
    public static ConfigurationManager Instance { get; } = new();

    // Now allow public construction
    public ConfigurationManager() { }

    public string GetSetting(string key) => /* ... */;
}

// Step 2: Register in DI
services.AddSingleton<IConfigurationManager, ConfigurationManager>();

// Step 3: Gradually migrate consumers to use injected dependency
// Step 4: Remove obsolete static Instance property
```

## When to Suppress

Suppression is appropriate when:

- The singleton is **truly stateless** and has no side effects
- You're working with a **third-party library** that requires the singleton pattern
- The class represents **hardware or system resources** that genuinely exist as a single instance
- You're in a **migration period** and have a plan to remove the singleton

```csharp
#pragma warning disable SEAM012
// This singleton manages a physical hardware resource
public sealed class PrinterSpooler
{
    public static PrinterSpooler Instance { get; } = new();
    private PrinterSpooler() { }
}
#pragma warning restore SEAM012
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM012.severity = none

# Or set to suggestion instead of warning
dotnet_diagnostic.SEAM012.severity = suggestion

# Exclude specific types
dotnet_code_quality.SEAM012.excluded_types = MyLegacySingleton, AnotherSingleton
```

## Related Rules

- [SEAM013](SEAM013.md) - Static Mutable Fields (related global state issue)
- [SEAM014](SEAM014.md) - Ambient Context (another form of global state)
- [SEAM003](SEAM003.md) - Service Locator (often used with singletons)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers - Chapter on "The Singleton Problem"
- [Dependency Injection Principles, Practices, and Patterns](https://www.amazon.com/Dependency-Injection-Principles-Practices-Patterns/dp/161729473X) by Steven van Deursen and Mark Seemann
- [Singleton is a Pathological Liar](https://testing.googleblog.com/2008/08/by-miko-hevery-so-you-join-new-project.html) by Misko Hevery
