using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.Infrastructure;

/// <summary>
/// Analyzer that detects direct Process.Start usage.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProcessStartAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ProcessStart);

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

        // Check for Process.Start static method
        if (fullTypeName == "System.Diagnostics.Process" && methodSymbol.Name == "Start")
        {
            ReportDiagnostic(context, invocation);
        }
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return;

        var fullTypeName = namedType.ToDisplayString();

        // Check for new Process() followed by Start() call
        if (fullTypeName == "System.Diagnostics.Process")
        {
            // Check if this is followed by a Start() call in the same expression
            if (IsFollowedByStartCall(objectCreation))
            {
                ReportDiagnostic(context, objectCreation);
            }
        }

        // Also check for ProcessStartInfo creation which suggests process launching
        if (fullTypeName == "System.Diagnostics.ProcessStartInfo")
        {
            ReportDiagnostic(context, objectCreation);
        }
    }

    private static bool IsFollowedByStartCall(ObjectCreationExpressionSyntax objectCreation)
    {
        // Check if the parent is a member access followed by Start invocation
        // e.g., new Process().Start() or var p = new Process(); p.Start();

        var parent = objectCreation.Parent;

        // Handle fluent call: new Process().Start()
        if (parent is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Name.Identifier.Text == "Start")
                return true;
        }

        return false;
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.ProcessStart,
            node.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}
