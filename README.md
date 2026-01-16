# TestHarness.Analyzers

[![CI](https://github.com/iirorahkonen/TestHarness.Analyzers/actions/workflows/ci.yml/badge.svg)](https://github.com/iirorahkonen/TestHarness.Analyzers/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/TestHarness.Analyzers.svg)](https://www.nuget.org/packages/TestHarness.Analyzers/)

A Roslyn analyzer library that detects code patterns blocking "seams" in legacy code, based on Michael Feathers' [Working Effectively with Legacy Code](https://www.oreilly.com/library/view/working-effectively-with/0131177052/).

## Use Cases

### 1. AI-Assisted Development

When SEAM rules are configured as warnings or errors, AI coding assistants (GitHub Copilot, Claude, Cursor, Windsurf, etc.) read build failures and adapt their code generation to avoid untestable patterns.

#### How It Works

```
Developer Request → AI Generation → Analyzer Feedback → AI Iteration → Testable Code
```

1. AI generates code with a hard dependency (e.g., `new EmailService()`)
2. Build fails with `SEAM001: Direct instantiation of concrete type 'EmailService'`
3. AI reads this feedback and regenerates using dependency injection
4. Resulting code is testable from the start

| Traditional Workflow | With Analyzers |
|----------------------|----------------|
| Write code → Analyze → Refactor | Configure rules → AI generates → Testable code |
| Fix problems after creation | Prevent problems during generation |

#### Setup

```xml
<!-- Directory.Build.props - enforce as errors -->
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

### 2. Legacy Code Refactoring

When working with legacy code, this analyzer helps you identify where to create seams so you can test a class. By detecting the 18 anti-patterns that block testability, you can systematically refactor code to introduce dependency injection points, extract interfaces, and break hard dependencies—making previously untestable code testable.

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

# Exclude namespaces from SEAM009
dotnet_code_quality.SEAM009.excluded_namespaces = MyNamespace.Internal

# Set complexity threshold for SEAM011 (default: 50)
dotnet_code_quality.SEAM011.complexity_threshold = 100
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
