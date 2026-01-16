using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.StaticDependencies;

/// <summary>
/// Analyzer that detects usage of Guid.NewGuid().
/// This creates non-deterministic dependencies that make testing difficult.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GuidNewGuidAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.GuidNewGuid);

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

        // Check for Guid.NewGuid()
        if (fullTypeName == "System.Guid" && methodSymbol.Name == "NewGuid")
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GuidNewGuid,
                invocation.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
