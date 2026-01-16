using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.Infrastructure;

/// <summary>
/// Analyzer that detects direct file system access that creates infrastructure dependencies.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileSystemAccessAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> FileSystemTypes = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileInfo",
        "System.IO.DirectoryInfo",
        "System.IO.FileStream",
        "System.IO.StreamReader",
        "System.IO.StreamWriter");

    private static readonly ImmutableHashSet<string> FileSystemMethods = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        // File methods
        "ReadAllText", "ReadAllLines", "ReadAllBytes", "ReadLines",
        "WriteAllText", "WriteAllLines", "WriteAllBytes",
        "AppendAllText", "AppendAllLines", "AppendText",
        "Create", "Delete", "Copy", "Move", "Exists", "Open", "OpenRead", "OpenWrite", "OpenText",
        "GetAttributes", "SetAttributes", "GetCreationTime", "SetCreationTime",
        "GetLastAccessTime", "SetLastAccessTime", "GetLastWriteTime", "SetLastWriteTime",
        // Directory methods
        "CreateDirectory", "GetFiles", "GetDirectories", "EnumerateFiles", "EnumerateDirectories",
        "GetCurrentDirectory", "SetCurrentDirectory");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.FileSystemAccess);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return;

        var fullTypeName = containingType.ToDisplayString();

        // Check if it's a file system operation
        if (!IsFileSystemType(fullTypeName))
            return;

        // Check if it's a method we care about
        if (methodSymbol.IsStatic && !FileSystemMethods.Contains(methodSymbol.Name))
            return;

        // Check excluded methods
        var excludedMethods = AnalyzerConfigOptions.GetExcludedMethods(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.FileSystemAccess);

        if (AnalyzerConfigOptions.IsMethodExcluded(methodSymbol, excludedMethods))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.FileSystemAccess,
            invocation.GetLocation(),
            $"{containingType.Name}.{methodSymbol.Name}");

        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return;

        var fullTypeName = namedType.ToDisplayString();

        // Check for direct instantiation of file system types
        if (fullTypeName is "System.IO.FileStream" or "System.IO.StreamReader" or "System.IO.StreamWriter" or
            "System.IO.FileInfo" or "System.IO.DirectoryInfo")
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.FileSystemAccess,
                objectCreation.GetLocation(),
                $"new {namedType.Name}");

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsFileSystemType(string fullTypeName)
    {
        return FileSystemTypes.Contains(fullTypeName) ||
               fullTypeName.StartsWith("System.IO.File", System.StringComparison.Ordinal) ||
               fullTypeName.StartsWith("System.IO.Directory", System.StringComparison.Ordinal);
    }
}
