# SEAM018: Direct Process.Start Usage

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM018 |
| **Category** | Infrastructure |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects direct usage of `Process.Start` and creation of `ProcessStartInfo` which creates dependencies on external processes and system state.

## Why This Is Problematic

Direct process execution causes testing challenges:

1. **External Dependencies**: Tests require specific executables to be installed
2. **System State**: Results depend on the system's PATH, environment, and installed software
3. **Side Effects**: Processes may modify files, network state, or other system resources
4. **Difficult to Mock**: Cannot easily simulate process behavior in unit tests
5. **Platform Differences**: Process behavior varies across Windows, Linux, macOS
6. **Security Concerns**: Executing processes opens potential security vulnerabilities
7. **Slow Tests**: Process execution is slow compared to mocked alternatives

## Examples

### Non-Compliant Code

```csharp
public class GitService
{
    public string GetCurrentBranch()
    {
        // Bad: Direct Process.Start
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "branch --show-current",
            RedirectStandardOutput = true,
            UseShellExecute = false
        });

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output.Trim();
    }
}
```

```csharp
public class PdfGenerator
{
    public void GeneratePdf(string htmlPath, string outputPath)
    {
        // Bad: Direct process execution
        Process.Start("wkhtmltopdf", $"{htmlPath} {outputPath}")?.WaitForExit();
    }
}
```

```csharp
public class SystemInfo
{
    public string GetHostname()
    {
        // Bad: ProcessStartInfo creation indicates process execution intent
        var startInfo = new ProcessStartInfo
        {
            FileName = "hostname",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        return process?.StandardOutput.ReadToEnd().Trim() ?? "";
    }
}
```

### Compliant Code

Using abstraction:

```csharp
public interface IProcessRunner
{
    ProcessResult Run(string fileName, string arguments);
    Task<ProcessResult> RunAsync(string fileName, string arguments,
        CancellationToken cancellationToken = default);
}

public record ProcessResult(int ExitCode, string Output, string Error);

public class ProcessRunner : IProcessRunner
{
    public ProcessResult Run(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    public async Task<ProcessResult> RunAsync(string fileName, string arguments,
        CancellationToken cancellationToken = default)
    {
        // Async implementation...
    }
}
```

Using the abstraction:

```csharp
public class GitService
{
    private readonly IProcessRunner _processRunner;

    public GitService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public string GetCurrentBranch()
    {
        var result = _processRunner.Run("git", "branch --show-current");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: {result.Error}");
        }

        return result.Output.Trim();
    }
}

// Test
[Fact]
public void GetCurrentBranch_ReturnsOutput()
{
    var mockRunner = new Mock<IProcessRunner>();
    mockRunner.Setup(r => r.Run("git", "branch --show-current"))
        .Returns(new ProcessResult(0, "main\n", ""));

    var service = new GitService(mockRunner.Object);

    var result = service.GetCurrentBranch();

    Assert.Equal("main", result);
}
```

Higher-level abstraction for specific tools:

```csharp
public interface IGitClient
{
    string GetCurrentBranch();
    IEnumerable<string> GetChangedFiles();
    void Commit(string message);
}

public class GitClient : IGitClient
{
    private readonly IProcessRunner _runner;

    public GitClient(IProcessRunner runner)
    {
        _runner = runner;
    }

    public string GetCurrentBranch()
    {
        var result = _runner.Run("git", "branch --show-current");
        return result.Output.Trim();
    }
}

// In tests, mock IGitClient directly
var mockGit = new Mock<IGitClient>();
mockGit.Setup(g => g.GetCurrentBranch()).Returns("feature-branch");
```

Using built-in APIs when possible:

```csharp
public class SystemInfo
{
    public string GetHostname()
    {
        // Good: Use .NET API instead of process
        return Environment.MachineName;
    }
}
```

## How to Fix

1. **Create Abstraction**: Define `IProcessRunner` or similar interface
2. **Implement Wrapper**: Create production implementation that calls `Process.Start`
3. **Inject Dependency**: Add the abstraction as a constructor parameter
4. **Use Built-in APIs**: Prefer .NET APIs over process execution when available
5. **Create Higher-Level Abstractions**: For specific tools (git, npm), create dedicated interfaces
6. **Mock in Tests**: Substitute the abstraction with test doubles

### DI Registration

```csharp
services.AddSingleton<IProcessRunner, ProcessRunner>();
services.AddScoped<IGitClient, GitClient>();
```

## When to Suppress

Suppression is appropriate when:

- You're implementing the **process runner abstraction** itself
- You're in **build scripts or CLI tools** where process execution is the core purpose
- You're writing **integration tests** that intentionally test process behavior
- You're in **startup code** checking for system requirements

```csharp
#pragma warning disable SEAM018
// This IS the adapter that wraps process execution
public class ProcessRunner : IProcessRunner
{
    public ProcessResult Run(string fileName, string arguments)
    {
        using var process = Process.Start(/*...*/);
        // ...
    }
}
#pragma warning restore SEAM018
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM018.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM018.severity = suggestion
```

## Related Rules

- [SEAM001](SEAM001.md) - Direct Instantiation
- [SEAM015](SEAM015.md) - File System Access
- [SEAM016](SEAM016.md) - HttpClient Creation
- [SEAM017](SEAM017.md) - Database Access

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Process Class Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process)
- [CliWrap Library](https://github.com/Tyrrrz/CliWrap) - A better way to interact with CLI applications
