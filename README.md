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

### Direct Dependencies
| Rule | Description |
|------|-------------|
| [SEAM001](docs/rules/SEAM001.md) | Direct instantiation of concrete types |
| [SEAM002](docs/rules/SEAM002.md) | Concrete type parameters without abstraction |
| [SEAM003](docs/rules/SEAM003.md) | Factory method returns concrete type |

### Static Dependencies
| Rule | Description |
|------|-------------|
| [SEAM004](docs/rules/SEAM004.md) | Static method calls |
| [SEAM005](docs/rules/SEAM005.md) | Static property access |
| [SEAM006](docs/rules/SEAM006.md) | Extension method dependency |
| [SEAM007](docs/rules/SEAM007.md) | Static event subscription |
| [SEAM008](docs/rules/SEAM008.md) | Ambient context usage |

### Inheritance Blockers
| Rule | Description |
|------|-------------|
| [SEAM009](docs/rules/SEAM009.md) | Sealed class prevents inheritance |
| [SEAM010](docs/rules/SEAM010.md) | Non-virtual method prevents override |
| [SEAM011](docs/rules/SEAM011.md) | Private method contains logic |

### Global State
| Rule | Description |
|------|-------------|
| [SEAM012](docs/rules/SEAM012.md) | Singleton pattern usage |
| [SEAM013](docs/rules/SEAM013.md) | Static mutable field |
| [SEAM014](docs/rules/SEAM014.md) | Service locator pattern |

### Infrastructure Dependencies
| Rule | Description |
|------|-------------|
| [SEAM015](docs/rules/SEAM015.md) | Direct file system access |
| [SEAM016](docs/rules/SEAM016.md) | Direct HTTP client usage |
| [SEAM017](docs/rules/SEAM017.md) | Direct database access |
| [SEAM018](docs/rules/SEAM018.md) | Direct process invocation |

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

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
