using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.StaticDependencies;

/// <summary>
/// Analyzer that detects direct usage of Environment.GetEnvironmentVariable.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnvironmentVariableAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.EnvironmentVariable);

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

        // Check for Environment.GetEnvironmentVariable and related methods
        if (fullTypeName == "System.Environment")
        {
            if (methodSymbol.Name is "GetEnvironmentVariable" or "GetEnvironmentVariables" or
                "SetEnvironmentVariable" or "ExpandEnvironmentVariables")
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.EnvironmentVariable,
                    invocation.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
