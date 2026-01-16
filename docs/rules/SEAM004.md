# SEAM004: Static Method Call Creates Untestable Dependency

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM004 |
| **Category** | StaticDependencies |
| **Severity** | Info |
| **Enabled** | No (opt-in) |

## Description

Detects calls to static methods on types like `File`, `Directory`, `Console`, `Debug`, `Trace`, `Thread`, and `Marshal` that create dependencies which cannot be easily substituted for testing.

## Why This Rule Is Disabled by Default

This rule is disabled by default because:

1. **High Volume**: Static method calls are extremely common in C# code
2. **Many Are Safe**: Pure functions like `Path.Combine` don't need abstraction
3. **Noise Level**: Enabling produces many diagnostics that may overwhelm
4. **Overlap with Other Rules**: More specific rules (SEAM005, SEAM007, SEAM015) cover common cases

Enable this rule when you want comprehensive static dependency detection.

## Detected Types

The analyzer flags calls to static methods on:

- `System.IO.File`
- `System.IO.Directory`
- `System.IO.Path` (except pure methods like Combine, GetFileName)
- `System.Console`
- `System.Diagnostics.Debug`
- `System.Diagnostics.Trace`
- `System.Threading.Thread`
- `System.Runtime.InteropServices.Marshal`

## Why This Is Problematic

Static method calls create testing challenges:

1. **Cannot Mock**: Static methods cannot be substituted without special frameworks
2. **Side Effects**: Many static methods have I/O or system side effects
3. **Global State**: Some static methods access or modify global state
4. **Hidden Dependencies**: The dependency on external systems isn't visible in the class API
5. **Non-Deterministic**: I/O operations may behave differently across environments

## Examples

### Non-Compliant Code

```csharp
public class DataProcessor
{
    public void ProcessData(string inputPath)
    {
        // Bad: Console.WriteLine for user output
        Console.WriteLine("Starting processing...");

        // Bad: File.ReadAllText creates I/O dependency
        var content = File.ReadAllText(inputPath);

        // Bad: Directory.Exists
        if (Directory.Exists("output"))
        {
            Directory.CreateDirectory("output");
        }

        ProcessContent(content);

        Console.WriteLine("Processing complete.");
    }
}
```

```csharp
public class DiagnosticLogger
{
    public void LogMessage(string message)
    {
        // Bad: Debug.WriteLine
        Debug.WriteLine($"[{DateTime.Now}] {message}");

        // Bad: Trace.TraceInformation
        Trace.TraceInformation(message);
    }
}
```

### Compliant Code

```csharp
public interface IConsole
{
    void WriteLine(string message);
    string? ReadLine();
}

public interface IFileSystem
{
    string ReadAllText(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
}

public class DataProcessor
{
    private readonly IConsole _console;
    private readonly IFileSystem _fileSystem;

    public DataProcessor(IConsole console, IFileSystem fileSystem)
    {
        _console = console;
        _fileSystem = fileSystem;
    }

    public void ProcessData(string inputPath)
    {
        _console.WriteLine("Starting processing...");

        var content = _fileSystem.ReadAllText(inputPath);

        if (!_fileSystem.DirectoryExists("output"))
        {
            _fileSystem.CreateDirectory("output");
        }

        ProcessContent(content);

        _console.WriteLine("Processing complete.");
    }
}
```

Using ILogger:

```csharp
public class DiagnosticLogger
{
    private readonly ILogger<DiagnosticLogger> _logger;

    public DiagnosticLogger(ILogger<DiagnosticLogger> logger)
    {
        _logger = logger;
    }

    public void LogMessage(string message)
    {
        _logger.LogDebug("{Message}", message);
        _logger.LogInformation("{Message}", message);
    }
}
```

## How to Fix

1. **Identify Static Calls**: Find all static method calls to the flagged types
2. **Create Abstraction**: Define an interface for the needed functionality
3. **Implement Wrapper**: Create a class that wraps the static calls
4. **Inject Dependency**: Add the interface as a constructor parameter
5. **Replace Calls**: Use the injected dependency instead of static calls
6. **Use Existing Abstractions**: Leverage `ILogger`, `System.IO.Abstractions`, etc.

### Pure Method Exceptions

These `Path` methods are NOT flagged (they're pure functions):

- `Path.Combine`
- `Path.GetFileName`
- `Path.GetFileNameWithoutExtension`
- `Path.GetExtension`
- `Path.GetDirectoryName`
- `Path.ChangeExtension`
- `Path.HasExtension`
- `Path.IsPathRooted`
- `Path.GetRelativePath`
- `Path.Join`
- `Path.GetPathRoot`

## How to Enable

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM004.severity = suggestion

# Exclude specific methods
dotnet_code_quality.SEAM004.excluded_methods = Console.WriteLine, Debug.Assert
```

## When to Suppress

Suppression is appropriate when:

- You're in **startup/bootstrapping code** that runs before DI is available
- The static call is for **debugging/development** only (Debug.Assert)
- You're in **test code** where the static call is acceptable
- You're implementing the **wrapper class** that abstracts the static calls

```csharp
#pragma warning disable SEAM004
// This IS the wrapper that calls the static methods
public class ConsoleWrapper : IConsole
{
    public void WriteLine(string message) => Console.WriteLine(message);
}
#pragma warning restore SEAM004
```

## Configuration

```ini
# .editorconfig

# Enable the rule
dotnet_diagnostic.SEAM004.severity = suggestion

# Exclude specific methods from analysis
dotnet_code_quality.SEAM004.excluded_methods = Debug.Assert, Path.Combine
```

## Related Rules

- [SEAM005](SEAM005.md) - DateTime.Now/UtcNow (specific static property)
- [SEAM006](SEAM006.md) - Guid.NewGuid (specific static method)
- [SEAM007](SEAM007.md) - Environment Variables
- [SEAM008](SEAM008.md) - Static Property Access
- [SEAM015](SEAM015.md) - File System Access

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [System.IO.Abstractions](https://github.com/TestableIO/System.IO.Abstractions)
- [Static Cling](https://deviq.com/antipatterns/static-cling) - Anti-pattern description
