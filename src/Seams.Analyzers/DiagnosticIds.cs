namespace Seams.Analyzers;

/// <summary>
/// Contains all diagnostic IDs for seam-blocking pattern analyzers.
/// Based on "Working Effectively with Legacy Code" by Michael Feathers.
/// </summary>
public static class DiagnosticIds
{
    // Category A: Direct Dependencies
    public const string DirectInstantiation = "SEAM001";
    public const string ConcreteConstructorParameter = "SEAM002";
    public const string ServiceLocator = "SEAM003";

    // Category B: Static Dependencies
    public const string StaticMethodCall = "SEAM004";
    public const string DateTimeNow = "SEAM005";
    public const string GuidNewGuid = "SEAM006";
    public const string EnvironmentVariable = "SEAM007";
    public const string StaticPropertyAccess = "SEAM008";

    // Category C: Inheritance Blockers
    public const string SealedClass = "SEAM009";
    public const string NonVirtualMethod = "SEAM010";
    public const string ComplexPrivateMethod = "SEAM011";

    // Category D: Global State
    public const string SingletonPattern = "SEAM012";
    public const string StaticMutableField = "SEAM013";
    public const string AmbientContext = "SEAM014";

    // Category E: Infrastructure Dependencies
    public const string FileSystemAccess = "SEAM015";
    public const string HttpClientCreation = "SEAM016";
    public const string DatabaseAccess = "SEAM017";
    public const string ProcessStart = "SEAM018";

    // Category F: Non-Deterministic Dependencies
    public const string RandomShared = "SEAM019";

    // Category G: Complexity
    public const string HighCyclomaticComplexity = "SEAM020";
}
