# TestHarness.Analyzers

[![CI](https://github.com/iirorahkonen/TestHarness.Analyzers/actions/workflows/ci.yml/badge.svg)](https://github.com/iirorahkonen/TestHarness.Analyzers/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/TestHarness.Analyzers.svg)](https://www.nuget.org/packages/TestHarness.Analyzers/)

A Roslyn analyzer library that detects code patterns blocking "seams" in legacy code, based on Michael Feathers' [Working Effectively with Legacy Code](https://www.oreilly.com/library/view/working-effectively-with/0131177052/).

## What are Seams?

A **seam** is a place where you can alter behavior in your program without editing in that place. Seams are essential for testing because they allow you to substitute dependencies, mock behaviors, and isolate code under test.

This analyzer identifies 18 anti-patterns (SEAM001-SEAM018) across 5 categories that hinder testability by blocking seams.

## Installation

```bash
dotnet add package TestHarness.Analyzers
```

Or via Package Manager:

```powershell
Install-Package TestHarness.Analyzers
```

## Diagnostic Rules

### Default Severity Levels

Not all rules are enabled by default. This follows the **Principle of Least Astonishment (POLA)** - rules that may produce many warnings in typical codebases are disabled by default to avoid overwhelming users on first installation.

| Severity | Meaning | Rules |
|----------|---------|-------|
| **Warning** | Enabled, likely issues | SEAM003, SEAM012, SEAM013, SEAM014, SEAM016 |
| **Info** | Enabled, suggestions | SEAM001, SEAM005, SEAM006, SEAM007, SEAM015, SEAM017, SEAM018 |
| **Disabled** | Opt-in only | SEAM002, SEAM004, SEAM008, SEAM009, SEAM010, SEAM011 |

To enable disabled rules, add to your `.editorconfig`:

```ini
# Enable specific disabled rules
dotnet_diagnostic.SEAM002.severity = suggestion
dotnet_diagnostic.SEAM004.severity = suggestion
dotnet_diagnostic.SEAM008.severity = suggestion
dotnet_diagnostic.SEAM009.severity = suggestion
dotnet_diagnostic.SEAM010.severity = suggestion
dotnet_diagnostic.SEAM011.severity = suggestion
```

### Direct Dependencies
| Rule | Description | Default |
|------|-------------|---------|
| [SEAM001](docs/rules/SEAM001.md) | Direct instantiation of concrete types | Info |
| [SEAM002](docs/rules/SEAM002.md) | Concrete type in constructor parameter | **Disabled** |
| [SEAM003](docs/rules/SEAM003.md) | Service locator pattern usage | Warning |

### Static Dependencies
| Rule | Description | Default |
|------|-------------|---------|
| [SEAM004](docs/rules/SEAM004.md) | Static method calls (File, Console, etc.) | **Disabled** |
| [SEAM005](docs/rules/SEAM005.md) | DateTime.Now/UtcNow usage | Info |
| [SEAM006](docs/rules/SEAM006.md) | Guid.NewGuid() usage | Info |
| [SEAM007](docs/rules/SEAM007.md) | Environment.GetEnvironmentVariable | Info |
| [SEAM008](docs/rules/SEAM008.md) | Static property access (ConfigurationManager) | **Disabled** |

### Inheritance Blockers
| Rule | Description | Default |
|------|-------------|---------|
| [SEAM009](docs/rules/SEAM009.md) | Sealed class prevents inheritance | **Disabled** |
| [SEAM010](docs/rules/SEAM010.md) | Non-virtual method prevents override | **Disabled** |
| [SEAM011](docs/rules/SEAM011.md) | Complex private method (50+ lines) | **Disabled** |

### Global State
| Rule | Description | Default |
|------|-------------|---------|
| [SEAM012](docs/rules/SEAM012.md) | Singleton pattern implementation | Warning |
| [SEAM013](docs/rules/SEAM013.md) | Static mutable field | Warning |
| [SEAM014](docs/rules/SEAM014.md) | Ambient context (HttpContext.Current, etc.) | Warning |

### Infrastructure Dependencies
| Rule | Description | Default |
|------|-------------|---------|
| [SEAM015](docs/rules/SEAM015.md) | Direct file system access | Info |
| [SEAM016](docs/rules/SEAM016.md) | Direct HttpClient creation | Warning |
| [SEAM017](docs/rules/SEAM017.md) | Direct database connection creation | Info |
| [SEAM018](docs/rules/SEAM018.md) | Direct Process.Start usage | Info |

## Configuration

Suppress diagnostics or exclude specific types via `.editorconfig`:

```ini
# Disable a rule entirely
dotnet_diagnostic.SEAM001.severity = none

# Exclude specific types from SEAM001
dotnet_code_quality.SEAM001.excluded_types = T:MyNamespace.AllowedFactory

# Exclude specific methods from SEAM004
dotnet_code_quality.SEAM004.excluded_methods = M:System.Console.WriteLine
```

## Example

```csharp
public class OrderService
{
    // SEAM001: Direct instantiation creates hard dependency
    private readonly EmailService _emailService = new EmailService();

    // Better: Inject the dependency
    private readonly IEmailService _emailService;

    public OrderService(IEmailService emailService)
    {
        _emailService = emailService;
    }
}
```

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](.github/CONTRIBUTING.md) for guidelines.

## Acknowledgments

This project is inspired by Michael Feathers' book [Working Effectively with Legacy Code](https://www.oreilly.com/library/view/working-effectively-with/0131177052/). The concept of "seams" as places where you can alter program behavior without editing in that place comes directly from his work. Thank you to Michael Feathers for providing such valuable insights into making legacy code testable.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
