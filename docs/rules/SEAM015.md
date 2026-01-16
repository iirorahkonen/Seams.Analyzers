# SEAM015: Direct File System Access

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM015 |
| **Category** | Infrastructure |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects direct file system access via `System.IO.File`, `System.IO.Directory`, `FileStream`, `StreamReader`, `StreamWriter`, `FileInfo`, and `DirectoryInfo` which creates infrastructure dependencies that are difficult to test.

## Why This Is Problematic

Direct file system access causes testing challenges:

1. **Requires Real Files**: Tests need actual files on disk, making them fragile and slow
2. **Cleanup Required**: Tests must clean up created files, or risk polluting the file system
3. **Permission Issues**: Tests may fail due to file system permissions
4. **Path Differences**: Paths differ between Windows, Linux, and macOS
5. **Parallel Test Conflicts**: Tests accessing the same files interfere with each other
6. **CI/CD Complexity**: Build servers may have different file system layouts
7. **Slow Tests**: Disk I/O is much slower than in-memory operations

## Examples

### Non-Compliant Code

```csharp
public class ConfigurationLoader
{
    public AppSettings LoadSettings()
    {
        // Bad: Direct File.ReadAllText
        var json = File.ReadAllText("config.json");
        return JsonSerializer.Deserialize<AppSettings>(json);
    }
}
```

```csharp
public class ReportExporter
{
    public void ExportReport(Report report, string path)
    {
        // Bad: Direct StreamWriter creation
        using var writer = new StreamWriter(path);
        writer.WriteLine(report.Title);

        foreach (var line in report.Lines)
        {
            writer.WriteLine(line);
        }
    }
}
```

```csharp
public class DataImporter
{
    public IEnumerable<string> GetFilesToProcess(string directory)
    {
        // Bad: Direct Directory.GetFiles
        return Directory.GetFiles(directory, "*.csv");
    }

    public void ProcessFile(string path)
    {
        // Bad: Direct FileInfo creation
        var info = new FileInfo(path);
        if (info.Length > 10_000_000)
        {
            throw new InvalidOperationException("File too large");
        }

        // Bad: Direct File.ReadLines
        foreach (var line in File.ReadLines(path))
        {
            Process(line);
        }
    }
}
```

### Compliant Code

Using `System.IO.Abstractions`:

```csharp
using System.IO.Abstractions;

public class ConfigurationLoader
{
    private readonly IFileSystem _fileSystem;

    public ConfigurationLoader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public AppSettings LoadSettings()
    {
        var json = _fileSystem.File.ReadAllText("config.json");
        return JsonSerializer.Deserialize<AppSettings>(json);
    }
}

// DI registration
services.AddSingleton<IFileSystem, FileSystem>();

// Test
[Fact]
public void LoadSettings_ReturnsDeserializedSettings()
{
    var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        ["config.json"] = new MockFileData("{\"Setting\": \"Value\"}")
    });

    var loader = new ConfigurationLoader(fileSystem);
    var result = loader.LoadSettings();

    Assert.Equal("Value", result.Setting);
}
```

Using custom abstraction:

```csharp
public interface IFileReader
{
    string ReadAllText(string path);
    IEnumerable<string> ReadLines(string path);
    bool Exists(string path);
}

public class FileReader : IFileReader
{
    public string ReadAllText(string path) => File.ReadAllText(path);
    public IEnumerable<string> ReadLines(string path) => File.ReadLines(path);
    public bool Exists(string path) => File.Exists(path);
}

public class DataImporter
{
    private readonly IFileReader _fileReader;

    public DataImporter(IFileReader fileReader)
    {
        _fileReader = fileReader;
    }

    public void ProcessFile(string path)
    {
        foreach (var line in _fileReader.ReadLines(path))
        {
            Process(line);
        }
    }
}
```

Using Stream abstraction:

```csharp
public class ReportExporter
{
    public void ExportReport(Report report, TextWriter writer)
    {
        // Good: Accept TextWriter instead of path
        writer.WriteLine(report.Title);
        foreach (var line in report.Lines)
        {
            writer.WriteLine(line);
        }
    }
}

// Usage
using var stream = new FileStream("report.txt", FileMode.Create);
using var writer = new StreamWriter(stream);
exporter.ExportReport(report, writer);

// Test
using var writer = new StringWriter();
exporter.ExportReport(report, writer);
Assert.Contains("Expected Title", writer.ToString());
```

## How to Fix

1. **Use System.IO.Abstractions**: Install the NuGet package and use `IFileSystem`
2. **Create Custom Abstraction**: Define interfaces for the specific operations you need
3. **Accept Streams/Writers**: Change methods to accept `Stream`, `TextReader`, `TextWriter`
4. **Inject Dependencies**: Add file system abstractions as constructor parameters
5. **Use MockFileSystem**: Leverage the mock implementation for testing

### NuGet Package

```bash
dotnet add package System.IO.Abstractions
dotnet add package System.IO.Abstractions.TestingHelpers
```

### DI Registration

```csharp
// Program.cs
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<IFile>(sp => sp.GetRequiredService<IFileSystem>().File);
services.AddSingleton<IDirectory>(sp => sp.GetRequiredService<IFileSystem>().Directory);
```

## When to Suppress

Suppression is appropriate when:

- You're implementing the **file system abstraction adapter** itself
- Code is in **startup/configuration** reading essential bootstrap files
- You're writing **CLI tools** where file I/O is the core purpose
- Tests are **integration tests** that intentionally verify file system behavior

```csharp
#pragma warning disable SEAM015
// This IS the adapter that wraps the file system
public class FileSystemAdapter : IFileReader
{
    public string ReadAllText(string path) => File.ReadAllText(path);
}
#pragma warning restore SEAM015
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM015.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM015.severity = suggestion

# Exclude specific methods
dotnet_code_quality.SEAM015.excluded_methods = File.Exists, Path.Combine
```

## Related Rules

- [SEAM004](SEAM004.md) - Static Method Calls (File.*, Directory.*)
- [SEAM016](SEAM016.md) - HttpClient Creation (similar infrastructure)
- [SEAM017](SEAM017.md) - Database Access (similar infrastructure)
- [SEAM018](SEAM018.md) - Process.Start (similar infrastructure)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [System.IO.Abstractions](https://github.com/TestableIO/System.IO.Abstractions) on GitHub
- [Mocking the File System](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices#mocking-the-file-system)
