using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.StaticDependencies;

/// <summary>
/// Analyzer that detects calls to static methods that create untestable dependencies.
/// Focuses on I/O operations like File, Console, Directory, etc.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StaticMethodCallAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ProblematicTypes = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.Path",
        "System.Console",
        "System.Diagnostics.Debug",
        "System.Diagnostics.Trace",
        "System.Threading.Thread",
        "System.Runtime.InteropServices.Marshal");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.StaticMethodCall);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!methodSymbol.IsStatic)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return;

        var fullTypeName = containingType.ToDisplayString();

        // Check if method is excluded
        var excludedMethods = AnalyzerConfigOptions.GetExcludedMethods(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.StaticMethodCall);

        if (AnalyzerConfigOptions.IsMethodExcluded(methodSymbol, excludedMethods))
            return;

        // Check if it's a problematic static method
        if (!ProblematicTypes.Contains(fullTypeName))
            return;

        // Skip Path methods that are pure functions (don't do I/O)
        if (fullTypeName == "System.IO.Path" && IsPurePathMethod(methodSymbol.Name))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.StaticMethodCall,
            invocation.GetLocation(),
            containingType.Name,
            methodSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsPurePathMethod(string methodName)
    {
        return methodName is "Combine" or "GetFileName" or "GetFileNameWithoutExtension" or
            "GetExtension" or "GetDirectoryName" or "ChangeExtension" or "HasExtension" or
            "IsPathRooted" or "GetRelativePath" or "Join" or "GetPathRoot";
    }
}
