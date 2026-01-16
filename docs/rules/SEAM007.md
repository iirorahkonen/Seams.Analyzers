# SEAM007: Environment Variables Create Configuration Dependency

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM007 |
| **Category** | StaticDependencies |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects direct access to environment variables via `Environment.GetEnvironmentVariable`, `Environment.SetEnvironmentVariable`, `Environment.GetEnvironmentVariables`, and `Environment.ExpandEnvironmentVariables`.

## Why This Is Problematic

Direct environment variable access causes testing issues:

1. **Shared Global State**: Environment variables are process-wide, affecting all tests
2. **Parallel Test Conflicts**: Tests modifying environment variables interfere with each other
3. **Machine-Specific Values**: Tests may fail on CI/CD servers with different environments
4. **Hidden Configuration**: Dependencies on specific variables aren't visible in the class API
5. **Cleanup Required**: Tests must remember to reset variables, leading to test pollution
6. **No Compile-Time Safety**: Missing variables only cause runtime errors

## Examples

### Non-Compliant Code

```csharp
public class DatabaseConfig
{
    public string GetConnectionString()
    {
        // Bad: Direct environment variable access
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var database = Environment.GetEnvironmentVariable("DB_NAME");

        return $"Host={host};Port={port};Database={database}";
    }
}
```

```csharp
public class FeatureManager
{
    public bool IsFeatureEnabled(string feature)
    {
        // Bad: Environment-based feature flags
        var value = Environment.GetEnvironmentVariable($"FEATURE_{feature.ToUpper()}");
        return value?.ToLower() == "true";
    }
}
```

```csharp
public class PathResolver
{
    public string GetDataPath()
    {
        // Bad: Using ExpandEnvironmentVariables
        return Environment.ExpandEnvironmentVariables("%APPDATA%\\MyApp\\Data");
    }
}
```

### Compliant Code

Using `IConfiguration`:

```csharp
public class DatabaseConfig
{
    private readonly IConfiguration _configuration;

    public DatabaseConfig(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        // Good: Using IConfiguration
        return _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string not configured");
    }
}

// In appsettings.json or environment variables via IConfiguration
// IConfiguration automatically reads from environment variables
```

Using Options pattern:

```csharp
public class DatabaseOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "";
}

public class DatabaseConfig
{
    private readonly DatabaseOptions _options;

    public DatabaseConfig(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public string GetConnectionString()
    {
        return $"Host={_options.Host};Port={_options.Port};Database={_options.Database}";
    }
}

// Configuration in Program.cs
services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
```

Using custom abstraction:

```csharp
public interface IEnvironmentVariables
{
    string? GetVariable(string name);
    void SetVariable(string name, string? value);
}

public class SystemEnvironmentVariables : IEnvironmentVariables
{
    public string? GetVariable(string name) =>
        Environment.GetEnvironmentVariable(name);

    public void SetVariable(string name, string? value) =>
        Environment.SetEnvironmentVariable(name, value);
}

// Test implementation
public class InMemoryEnvironmentVariables : IEnvironmentVariables
{
    private readonly Dictionary<string, string?> _variables = new();

    public string? GetVariable(string name) =>
        _variables.TryGetValue(name, out var value) ? value : null;

    public void SetVariable(string name, string? value) =>
        _variables[name] = value;
}
```

Feature flags with proper abstraction:

```csharp
public interface IFeatureFlags
{
    bool IsEnabled(string feature);
}

public class FeatureFlags : IFeatureFlags
{
    private readonly IConfiguration _configuration;

    public FeatureFlags(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEnabled(string feature)
    {
        return _configuration.GetValue<bool>($"Features:{feature}");
    }
}
```

## How to Fix

1. **Use IConfiguration**: Leverage ASP.NET Core's configuration system
2. **Use Options Pattern**: Create strongly-typed configuration classes
3. **Create Abstraction**: If you need direct env var access, wrap in an interface
4. **Register Configuration**: Set up configuration sources in Program.cs
5. **Test with In-Memory Config**: Use `ConfigurationBuilder` with `AddInMemoryCollection`

### Configuration Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// IConfiguration automatically reads from:
// - appsettings.json
// - appsettings.{Environment}.json
// - Environment variables
// - Command line args

// Access via DI
services.Configure<MyOptions>(builder.Configuration.GetSection("MyOptions"));
```

### Testing with IConfiguration

```csharp
[Fact]
public void GetConnectionString_ReturnsConfiguredValue()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["ConnectionStrings:Default"] = "Host=testhost;Port=5432;Database=testdb"
        })
        .Build();

    var config = new DatabaseConfig(configuration);

    // Act
    var result = config.GetConnectionString();

    // Assert
    Assert.Equal("Host=testhost;Port=5432;Database=testdb", result);
}
```

## When to Suppress

Suppression is appropriate when:

- You're in **application startup** reading initial configuration
- You're implementing the **IConfiguration provider** that reads environment variables
- You're writing **CLI tools** where environment variables are the expected interface
- You're in **test setup code** intentionally setting environment for integration tests

```csharp
#pragma warning disable SEAM007
// This IS the configuration adapter that reads from environment
public class EnvironmentConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            Data[entry.Key.ToString()] = entry.Value?.ToString();
        }
    }
}
#pragma warning restore SEAM007
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM007.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM007.severity = suggestion
```

## Related Rules

- [SEAM008](SEAM008.md) - Static Property Access (ConfigurationManager)
- [SEAM004](SEAM004.md) - Static Method Calls
- [SEAM014](SEAM014.md) - Ambient Context

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Options Pattern in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/options)
