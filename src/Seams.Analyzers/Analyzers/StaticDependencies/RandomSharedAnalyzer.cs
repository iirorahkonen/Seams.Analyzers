using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.StaticDependencies;

/// <summary>
/// Analyzer that detects usage of Random.Shared.
/// This creates non-deterministic dependencies that make testing difficult.
/// Random.Shared was introduced in .NET 6.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RandomSharedAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.RandomShared);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if the invocation is a method call on Random.Shared
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        // Check if the method is called on System.Random
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return;

        var fullTypeName = containingType.ToDisplayString();
        if (fullTypeName != "System.Random")
            return;

        // Check if this is called on Random.Shared (static property access)
        if (memberAccess.Expression is not MemberAccessExpressionSyntax sharedAccess)
            return;

        var sharedSymbolInfo = context.SemanticModel.GetSymbolInfo(sharedAccess, context.CancellationToken);
        if (sharedSymbolInfo.Symbol is IPropertySymbol { IsStatic: true, Name: "Shared" } propertySymbol &&
            propertySymbol.ContainingType?.ToDisplayString() == "System.Random")
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.RandomShared,
                invocation.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
