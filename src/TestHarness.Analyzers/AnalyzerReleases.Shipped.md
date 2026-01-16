; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SEAM001 | DirectDependencies | Info | Direct instantiation of concrete type
SEAM002 | DirectDependencies | Disabled | Concrete type in constructor parameter
SEAM003 | DirectDependencies | Warning | Service locator pattern usage
SEAM004 | StaticDependencies | Disabled | Static method call creates untestable dependency
SEAM005 | StaticDependencies | Info | DateTime.Now/UtcNow creates non-deterministic dependency
SEAM006 | StaticDependencies | Info | Guid.NewGuid creates non-deterministic dependency
SEAM007 | StaticDependencies | Info | Environment.GetEnvironmentVariable creates configuration dependency
SEAM008 | StaticDependencies | Disabled | Static property access creates untestable dependency
SEAM009 | InheritanceBlockers | Disabled | Sealed class prevents inheritance seam
SEAM010 | InheritanceBlockers | Disabled | Non-virtual method prevents override seam
SEAM011 | InheritanceBlockers | Disabled | Complex private method should be extracted
SEAM012 | GlobalState | Warning | Singleton pattern creates global state
SEAM013 | GlobalState | Warning | Static mutable field creates shared state
SEAM014 | GlobalState | Warning | Ambient context creates hidden dependency
SEAM015 | Infrastructure | Info | Direct file system access
SEAM016 | Infrastructure | Warning | Direct HttpClient creation
SEAM017 | Infrastructure | Info | Direct database connection creation
SEAM018 | Infrastructure | Info | Direct Process.Start usage
