using Microsoft.CodeAnalysis;

namespace TestHarness.Analyzers;

/// <summary>
/// Contains all diagnostic descriptors for seam-blocking pattern analyzers.
/// </summary>
public static class DiagnosticDescriptors
{
    private const string HelpLinkBase = "https://github.com/iirorahkonen/TestHarness.Analyzers/blob/main/docs/rules/";

    // Categories
    private const string DirectDependenciesCategory = "DirectDependencies";
    private const string StaticDependenciesCategory = "StaticDependencies";
    private const string InheritanceBlockersCategory = "InheritanceBlockers";
    private const string GlobalStateCategory = "GlobalState";
    private const string InfrastructureCategory = "Infrastructure";

    // Category A: Direct Dependencies

    public static readonly DiagnosticDescriptor DirectInstantiation = new(
        DiagnosticIds.DirectInstantiation,
        title: "Direct instantiation of concrete type",
        messageFormat: "Direct instantiation of '{0}' creates a hard dependency that prevents seam injection",
        category: DirectDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Direct instantiation of concrete types prevents dependency injection and makes code difficult to test. Consider extracting to a constructor parameter or factory method.",
        helpLinkUri: HelpLinkBase + "SEAM001.md");

    public static readonly DiagnosticDescriptor ConcreteConstructorParameter = new(
        DiagnosticIds.ConcreteConstructorParameter,
        title: "Concrete type in constructor parameter",
        messageFormat: "Constructor parameter '{0}' uses concrete type '{1}' instead of an abstraction",
        category: DirectDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Using concrete types in constructor parameters reduces flexibility. Consider depending on an interface or abstract class instead.",
        helpLinkUri: HelpLinkBase + "SEAM002.md");

    public static readonly DiagnosticDescriptor ServiceLocator = new(
        DiagnosticIds.ServiceLocator,
        title: "Service locator pattern usage",
        messageFormat: "Service locator pattern hides dependencies and makes code harder to test",
        category: DirectDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The service locator pattern hides dependencies, making them implicit rather than explicit. Consider using constructor injection instead.",
        helpLinkUri: HelpLinkBase + "SEAM003.md");

    // Category B: Static Dependencies

    public static readonly DiagnosticDescriptor StaticMethodCall = new(
        DiagnosticIds.StaticMethodCall,
        title: "Static method call creates untestable dependency",
        messageFormat: "Static method '{0}.{1}' creates a dependency that cannot be easily substituted for testing",
        category: StaticDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Calls to static methods like File.ReadAllText, Console.WriteLine create hard dependencies. Consider wrapping in an abstraction.",
        helpLinkUri: HelpLinkBase + "SEAM004.md");

    public static readonly DiagnosticDescriptor DateTimeNow = new(
        DiagnosticIds.DateTimeNow,
        title: "DateTime.Now/UtcNow creates non-deterministic dependency",
        messageFormat: "'{0}' creates a non-deterministic dependency that makes testing difficult",
        category: StaticDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Using DateTime.Now or DateTime.UtcNow directly makes code non-deterministic and hard to test. Consider injecting ITimeProvider or TimeProvider (.NET 8+).",
        helpLinkUri: HelpLinkBase + "SEAM005.md");

    public static readonly DiagnosticDescriptor GuidNewGuid = new(
        DiagnosticIds.GuidNewGuid,
        title: "Guid.NewGuid creates non-deterministic dependency",
        messageFormat: "Guid.NewGuid() creates a non-deterministic dependency that makes testing difficult",
        category: StaticDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Using Guid.NewGuid() directly makes code non-deterministic and hard to test. Consider injecting an IGuidGenerator abstraction.",
        helpLinkUri: HelpLinkBase + "SEAM006.md");

    public static readonly DiagnosticDescriptor EnvironmentVariable = new(
        DiagnosticIds.EnvironmentVariable,
        title: "Environment.GetEnvironmentVariable creates configuration dependency",
        messageFormat: "Environment.GetEnvironmentVariable creates a hard dependency on environment configuration",
        category: StaticDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Direct access to environment variables makes code dependent on runtime environment. Consider injecting IConfiguration.",
        helpLinkUri: HelpLinkBase + "SEAM007.md");

    public static readonly DiagnosticDescriptor StaticPropertyAccess = new(
        DiagnosticIds.StaticPropertyAccess,
        title: "Static property access creates untestable dependency",
        messageFormat: "Static property '{0}.{1}' creates a dependency that cannot be easily substituted for testing",
        category: StaticDependenciesCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Access to static properties like ConfigurationManager.AppSettings creates hard dependencies. Consider injecting IConfiguration.",
        helpLinkUri: HelpLinkBase + "SEAM008.md");

    // Category C: Inheritance Blockers

    public static readonly DiagnosticDescriptor SealedClass = new(
        DiagnosticIds.SealedClass,
        title: "Sealed class prevents inheritance seam",
        messageFormat: "Sealed class '{0}' cannot be subclassed for testing purposes",
        category: InheritanceBlockersCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Sealed classes cannot be subclassed, preventing the use of subclass and override testing techniques. Consider removing the sealed modifier if the class needs to be extended for testing.",
        helpLinkUri: HelpLinkBase + "SEAM009.md");

    public static readonly DiagnosticDescriptor NonVirtualMethod = new(
        DiagnosticIds.NonVirtualMethod,
        title: "Non-virtual method prevents override seam",
        messageFormat: "Non-virtual method '{0}' cannot be overridden for testing purposes",
        category: InheritanceBlockersCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Non-virtual methods cannot be overridden in subclasses, preventing the use of subclass and override testing techniques. Consider making the method virtual.",
        helpLinkUri: HelpLinkBase + "SEAM010.md");

    public static readonly DiagnosticDescriptor ComplexPrivateMethod = new(
        DiagnosticIds.ComplexPrivateMethod,
        title: "Complex private method should be extracted",
        messageFormat: "Private method '{0}' has {1} lines and should be extracted to a separate testable class",
        category: InheritanceBlockersCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Large private methods contain logic that cannot be tested in isolation. Consider extracting the logic to a separate class that can be injected and tested independently.",
        helpLinkUri: HelpLinkBase + "SEAM011.md");

    // Category D: Global State

    public static readonly DiagnosticDescriptor SingletonPattern = new(
        DiagnosticIds.SingletonPattern,
        title: "Singleton pattern creates global state",
        messageFormat: "Singleton pattern in '{0}' creates global state that persists across tests",
        category: GlobalStateCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The singleton pattern creates global state that can cause test pollution and makes code harder to test in isolation. Consider using dependency injection container registration instead.",
        helpLinkUri: HelpLinkBase + "SEAM012.md");

    public static readonly DiagnosticDescriptor StaticMutableField = new(
        DiagnosticIds.StaticMutableField,
        title: "Static mutable field creates shared state",
        messageFormat: "Static mutable field '{0}' creates shared state that can cause test pollution",
        category: GlobalStateCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Static mutable fields create shared state that persists across tests and can cause unpredictable behavior. Consider converting to instance fields.",
        helpLinkUri: HelpLinkBase + "SEAM013.md");

    public static readonly DiagnosticDescriptor AmbientContext = new(
        DiagnosticIds.AmbientContext,
        title: "Ambient context creates hidden dependency",
        messageFormat: "Ambient context '{0}' creates a hidden dependency that is difficult to control in tests",
        category: GlobalStateCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Ambient contexts like HttpContext.Current create hidden dependencies that are difficult to set up in tests. Consider injecting IHttpContextAccessor instead.",
        helpLinkUri: HelpLinkBase + "SEAM014.md");

    // Category E: Infrastructure Dependencies

    public static readonly DiagnosticDescriptor FileSystemAccess = new(
        DiagnosticIds.FileSystemAccess,
        title: "Direct file system access",
        messageFormat: "Direct file system access via '{0}' creates an infrastructure dependency",
        category: InfrastructureCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Direct file system access makes code dependent on the file system and harder to test. Consider injecting an IFileSystem abstraction.",
        helpLinkUri: HelpLinkBase + "SEAM015.md");

    public static readonly DiagnosticDescriptor HttpClientCreation = new(
        DiagnosticIds.HttpClientCreation,
        title: "Direct HttpClient creation",
        messageFormat: "Direct HttpClient creation should be avoided in favor of IHttpClientFactory",
        category: InfrastructureCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Creating HttpClient directly can lead to socket exhaustion and makes testing difficult. Use IHttpClientFactory instead.",
        helpLinkUri: HelpLinkBase + "SEAM016.md");

    public static readonly DiagnosticDescriptor DatabaseAccess = new(
        DiagnosticIds.DatabaseAccess,
        title: "Direct database connection creation",
        messageFormat: "Direct creation of '{0}' creates tight coupling to database infrastructure",
        category: InfrastructureCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Creating database connections directly makes code tightly coupled to the database and hard to test. Consider using the repository pattern with dependency injection.",
        helpLinkUri: HelpLinkBase + "SEAM017.md");

    public static readonly DiagnosticDescriptor ProcessStart = new(
        DiagnosticIds.ProcessStart,
        title: "Direct Process.Start usage",
        messageFormat: "Direct Process.Start creates dependency on external processes",
        category: InfrastructureCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Calling Process.Start directly creates a dependency on external processes and system state. Consider injecting an IProcessRunner abstraction.",
        helpLinkUri: HelpLinkBase + "SEAM018.md");
}
