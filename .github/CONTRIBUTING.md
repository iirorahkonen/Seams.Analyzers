# Contributing to TestHarness.Analyzers

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR-USERNAME/TestHarness.Analyzers.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit your changes with a descriptive message
7. Push to your fork and submit a Pull Request

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- An IDE with Roslyn support (Visual Studio, Rider, or VS Code with C# extension)

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running a Single Test

```bash
dotnet test --filter "FullyQualifiedName~YourTestName"
```

## Adding a New Analyzer

1. Add diagnostic ID constant to `src/TestHarness.Analyzers/DiagnosticIds.cs`
2. Add diagnostic descriptor to `src/TestHarness.Analyzers/DiagnosticDescriptors.cs`
3. Create analyzer class in appropriate `src/TestHarness.Analyzers/Analyzers/` subfolder
4. Create code fix in parallel `src/TestHarness.Analyzers/CodeFixes/` subfolder (optional)
5. Add tests in `tests/TestHarness.Analyzers.Tests/AnalyzerTests/`
6. Add documentation in `docs/rules/SEAMxxx.md`
7. Update `src/TestHarness.Analyzers/AnalyzerReleases.Unshipped.md`

## Code Style

- All analyzers must be `sealed`
- Enable concurrent execution in analyzers: `context.EnableConcurrentExecution()`
- Configure generated code analysis appropriately
- Place private methods after public ones
- Use raw string literals (`"""`) for test source code

## Commit Messages

- Use clear, descriptive commit messages
- For version bumps, include `+semver: minor` or `+semver: major` in the commit message

## Pull Request Guidelines

- Ensure all tests pass
- Update documentation if adding new rules
- Add tests for new functionality
- Keep PRs focused on a single concern

## Reporting Issues

- Use the issue templates when available
- Include steps to reproduce for bugs
- Provide code samples when relevant

## Questions?

Feel free to open an issue for questions or discussions.
