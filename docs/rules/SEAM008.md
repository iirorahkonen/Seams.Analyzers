# SEAM008: Static Property Access Creates Untestable Dependency

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM008 |
| **Category** | StaticDependencies |
| **Severity** | Info |
| **Enabled** | No (opt-in) |

## Description

Detects access to static properties like `ConfigurationManager.AppSettings`, `Environment.CurrentDirectory`, `Thread.CurrentThread`, and `CultureInfo.CurrentCulture` that create dependencies which cannot be easily substituted for testing.

## Why This Rule Is Disabled by Default

This rule is disabled by default because:

1. **Overlap with Other Rules**: SEAM007 and SEAM014 cover many common cases
2. **Legacy Code Focus**: Primarily relevant for older .NET Framework code
3. **Modern Alternatives**: ASP.NET Core uses IConfiguration, reducing relevance
4. **Selective Enablement**: Teams may want to enable only for specific cases

Enable this rule when working with legacy code using `ConfigurationManager` or similar patterns.

## Detected Patterns

The analyzer flags these static property accesses:

| Type | Properties |
|------|------------|
| `ConfigurationManager` | `AppSettings`, `ConnectionStrings` |
| `Environment` | `CurrentDirectory`, `MachineName`, `UserName`, `OSVersion` |
| `Thread` | `CurrentThread`, `CurrentPrincipal` |
| `CultureInfo` | `CurrentCulture`, `CurrentUICulture` |

## Why This Is Problematic

Static property access causes testing challenges:

1. **Global State**: Properties may return different values based on system state
2. **Cannot Mock**: Static properties cannot be substituted in tests
3. **Machine-Specific**: Values differ between development and CI machines
4. **Side Effects**: Some setters modify global state affecting other tests
5. **Hidden Dependencies**: The dependency on system state isn't visible in the API

## Examples

### Non-Compliant Code

```csharp
public class AppSettings
{
    public string GetApiKey()
    {
        // Bad: ConfigurationManager.AppSettings access
        return ConfigurationManager.AppSettings["ApiKey"]
            ?? throw new InvalidOperationException("ApiKey not configured");
    }

    public string GetConnectionString()
    {
        // Bad: ConfigurationManager.ConnectionStrings access
        return ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
    }
}
```

```csharp
public class FileManager
{
    public string GetWorkingPath()
    {
        // Bad: Environment.CurrentDirectory access
        return Path.Combine(Environment.CurrentDirectory, "data");
    }
}
```

```csharp
public class CultureService
{
    public string FormatCurrency(decimal amount)
    {
        // Bad: CultureInfo.CurrentCulture access
        return amount.ToString("C", CultureInfo.CurrentCulture);
    }
}
```

### Compliant Code

Using IConfiguration (modern .NET):

```csharp
public class AppSettings
{
    private readonly IConfiguration _configuration;

    public AppSettings(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetApiKey()
    {
        return _configuration["ApiKey"]
            ?? throw new InvalidOperationException("ApiKey not configured");
    }

    public string GetConnectionString()
    {
        return _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string not configured");
    }
}
```

Using custom abstraction:

```csharp
public interface IEnvironmentInfo
{
    string CurrentDirectory { get; }
    string MachineName { get; }
    string UserName { get; }
}

public class EnvironmentInfo : IEnvironmentInfo
{
    public string CurrentDirectory => Environment.CurrentDirectory;
    public string MachineName => Environment.MachineName;
    public string UserName => Environment.UserName;
}

public class FileManager
{
    private readonly IEnvironmentInfo _environment;

    public FileManager(IEnvironmentInfo environment)
    {
        _environment = environment;
    }

    public string GetWorkingPath()
    {
        return Path.Combine(_environment.CurrentDirectory, "data");
    }
}
```

Passing culture explicitly:

```csharp
public class CultureService
{
    public string FormatCurrency(decimal amount, CultureInfo? culture = null)
    {
        // Good: Culture passed as parameter, defaults to invariant for consistency
        culture ??= CultureInfo.InvariantCulture;
        return amount.ToString("C", culture);
    }
}
```

## How to Fix

1. **Migrate to IConfiguration**: For config access, use ASP.NET Core's configuration system
2. **Create Abstractions**: Define interfaces for environment/system information
3. **Pass Values Explicitly**: Instead of accessing global state, pass values as parameters
4. **Inject Dependencies**: Add abstractions as constructor parameters
5. **Use Options Pattern**: For strongly-typed configuration

### Migration from ConfigurationManager

```csharp
// Before (.NET Framework)
var value = ConfigurationManager.AppSettings["Setting"];

// After (modern .NET)
// In appsettings.json: { "Setting": "value" }

public class MyService
{
    private readonly IConfiguration _config;

    public MyService(IConfiguration config)
    {
        _config = config;
    }

    public void DoWork()
    {
        var value = _config["Setting"];
    }
}
```

## How to Enable

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM008.severity = suggestion

# Or as a warning for stricter enforcement
dotnet_diagnostic.SEAM008.severity = warning
```

## When to Suppress

Suppression is appropriate when:

- You're implementing the **abstraction wrapper** itself
- The code is in **startup/bootstrapping** before DI is available
- You're in **legacy code** with a migration plan
- The property access is for **logging/debugging** only

```csharp
#pragma warning disable SEAM008
// This IS the adapter wrapping static properties
public class EnvironmentInfo : IEnvironmentInfo
{
    public string CurrentDirectory => Environment.CurrentDirectory;
}
#pragma warning restore SEAM008
```

## Configuration

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM008.severity = suggestion
```

## Related Rules

- [SEAM007](SEAM007.md) - Environment Variables (related pattern)
- [SEAM014](SEAM014.md) - Ambient Context (similar static access)
- [SEAM004](SEAM004.md) - Static Method Calls

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Options Pattern in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/options)
