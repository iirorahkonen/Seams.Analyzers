# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Seams.Analyzers is a Roslyn analyzer library that detects code patterns blocking "seams" in legacy code, based on Michael Feathers' "Working Effectively with Legacy Code." It identifies 18 anti-patterns (SEAM001-SEAM018) across 5 categories that hinder testability.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run a single test by name
dotnet test --filter "FullyQualifiedName~DirectInstantiationAnalyzerTests.DirectInstantiation_List_ShouldNotReportDiagnostic"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Pack the analyzer as NuGet package
dotnet pack src/Seams.Analyzers/Seams.Analyzers.csproj
```

## Architecture

### Project Structure
- `src/Seams.Analyzers/` - Main analyzer library (netstandard2.0 for compatibility)
  - `Analyzers/` - Diagnostic implementations organized by category
  - `CodeFixes/` - Code fix providers (parallel structure to Analyzers)
  - `DiagnosticIds.cs` - All rule ID constants
  - `DiagnosticDescriptors.cs` - Rule definitions with metadata
  - `AnalyzerConfigOptions.cs` - EditorConfig support for exclusions
- `tests/Seams.Analyzers.Tests/` - xUnit tests (net8.0)
  - `Verifiers/` - Test infrastructure wrapping Microsoft's analyzer testing framework
- `tests/Seams.Analyzers.Samples/` - Sample code demonstrating violations
- `docs/rules/` - Markdown documentation for each rule

### Diagnostic Categories
| Category | Rules | Purpose |
|----------|-------|---------|
| DirectDependencies | SEAM001-003 | Hard coupling through instantiation |
| StaticDependencies | SEAM004-008 | Static method/property dependencies |
| InheritanceBlockers | SEAM009-011 | Sealed classes, non-virtual methods |
| GlobalState | SEAM012-014 | Singletons, static mutable fields |
| Infrastructure | SEAM015-018 | File system, HTTP, database, process access |

### Key Patterns

**Analyzer Implementation:**
- Extend `DiagnosticAnalyzer` with `[DiagnosticAnalyzer(LanguageNames.CSharp)]`
- Register syntax node actions in `Initialize()` method
- Use `context.RegisterSyntaxNodeAction()` for specific syntax kinds

**Test Pattern:**
```csharp
[Fact]
public async Task TestName()
{
    const string source = """
        // Test code here
        """;

    // No expected diagnostics = passes if no warnings
    await CSharpAnalyzerVerifier<AnalyzerType>.VerifyAnalyzerAsync(source);

    // With expected diagnostic
    var expected = CSharpAnalyzerVerifier<AnalyzerType>.Diagnostic("SEAM001")
        .WithSpan(lineStart, columnStart, lineEnd, columnEnd)
        .WithArguments("TypeName");
    await CSharpAnalyzerVerifier<AnalyzerType>.VerifyAnalyzerAsync(source, expected);
}
```

**Configuration via .editorconfig:**
```ini
dotnet_diagnostic.SEAM001.severity = none
dotnet_code_quality.SEAM001.excluded_types = T:MyNamespace.AllowedFactory
dotnet_code_quality.SEAM004.excluded_methods = M:System.Console.WriteLine
```

## Adding a New Analyzer

1. Add diagnostic ID constant to `DiagnosticIds.cs`
2. Add diagnostic descriptor to `DiagnosticDescriptors.cs`
3. Create analyzer class in appropriate `Analyzers/` subfolder
4. Create code fix in parallel `CodeFixes/` subfolder (optional)
5. Add tests in `tests/Seams.Analyzers.Tests/AnalyzerTests/`
6. Add documentation in `docs/rules/SEAMxxx.md`
7. Update `AnalyzerReleases.Unshipped.md`

## Analyzer Release Tracking

The project uses two files to track diagnostic rule changes for releases:

| File | When to Update |
|------|----------------|
| `AnalyzerReleases.Unshipped.md` | When adding, modifying, or removing diagnostic rules |
| `AnalyzerReleases.Shipped.md` | At release time only |

**Adding/Modifying Rules (Unshipped.md):**
- Add new rules to the `### New Rules` table
- Format: `Rule ID | Category | Severity | Notes`
- Severity should be `Info`, `Warning`, `Error`, or `Disabled`

**At Release Time:**
1. Move content from `Unshipped.md` to `Shipped.md`
2. Add a version header: `## Release X.Y.Z`
3. Clear `Unshipped.md` (keep only the header comments)

## Code Style

- All analyzers must be `sealed`
- Enable concurrent execution: `context.EnableConcurrentExecution()`
- Configure generated code analysis: `context.ConfigureGeneratedCodeAnalysis(...)`
- Private methods after public ones
- Use raw string literals for test source code

## Branching Strategy

This project uses GitVersion for semantic versioning with the following branch conventions:

| Branch Pattern | Purpose | Version Tag |
|----------------|---------|-------------|
| `main` | Production releases | Release version |
| `feature/*` | New features | `-alpha` |
| `hotfix/*` | Urgent fixes | `-beta` |

### Workflow
1. Create feature branches from `main` using `feature/description` naming
2. Open PR targeting `main`
3. CI runs on all PRs and main branch pushes
4. Merge to `main` triggers versioned build and NuGet package

### Version Bumping
- **Default**: Patch increment (0.1.0 → 0.1.1)
- **Minor bump**: Include `+semver: minor` in commit message (0.1.0 → 0.2.0)
- **Major bump**: Include `+semver: major` in commit message (0.1.0 → 1.0.0)
