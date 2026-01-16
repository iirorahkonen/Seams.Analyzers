using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.StaticDependencies;

/// <summary>
/// Analyzer that detects usage of DateTime.Now, DateTime.UtcNow, DateTimeOffset.Now, and DateTimeOffset.UtcNow.
/// These create non-deterministic dependencies that make testing difficult.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DateTimeNowAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DateTimeNow);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        if (symbolInfo.Symbol is not IPropertySymbol propertySymbol)
            return;

        if (!propertySymbol.IsStatic)
            return;

        var containingType = propertySymbol.ContainingType;
        if (containingType == null)
            return;

        var fullTypeName = containingType.ToDisplayString();
        var propertyName = propertySymbol.Name;

        // Check for DateTime.Now, DateTime.UtcNow, DateTime.Today
        if (fullTypeName == "System.DateTime")
        {
            if (propertyName is "Now" or "UtcNow" or "Today")
            {
                ReportDiagnostic(context, memberAccess, $"DateTime.{propertyName}");
                return;
            }
        }

        // Check for DateTimeOffset.Now, DateTimeOffset.UtcNow
        if (fullTypeName == "System.DateTimeOffset")
        {
            if (propertyName is "Now" or "UtcNow")
            {
                ReportDiagnostic(context, memberAccess, $"DateTimeOffset.{propertyName}");
            }
        }
    }

    private static void ReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax memberAccess,
        string accessedMember)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.DateTimeNow,
            memberAccess.GetLocation(),
            accessedMember);

        context.ReportDiagnostic(diagnostic);
    }
}
