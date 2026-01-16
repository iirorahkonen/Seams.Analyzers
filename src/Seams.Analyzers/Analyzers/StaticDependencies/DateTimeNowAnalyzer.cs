using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.StaticDependencies;

/// <summary>
/// Analyzer that detects usage of DateTime.Now, DateTime.UtcNow, DateTimeOffset.Now, DateTimeOffset.UtcNow,
/// and TimeProvider.System.GetUtcNow()/GetLocalNow().
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
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
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

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if the invocation is a method call on TimeProvider.System
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        // Check if this is a call to GetUtcNow or GetLocalNow on TimeProvider
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return;

        var fullTypeName = containingType.ToDisplayString();

        // Check for TimeProvider.GetUtcNow() and TimeProvider.GetLocalNow()
        if (fullTypeName == "System.TimeProvider" &&
            methodSymbol.Name is "GetUtcNow" or "GetLocalNow")
        {
            // Verify this is called on TimeProvider.System (static property access)
            if (memberAccess.Expression is MemberAccessExpressionSyntax systemAccess)
            {
                var systemSymbolInfo = context.SemanticModel.GetSymbolInfo(systemAccess, context.CancellationToken);
                if (systemSymbolInfo.Symbol is IPropertySymbol { IsStatic: true, Name: "System" })
                {
                    ReportDiagnosticForInvocation(context, invocation, $"TimeProvider.System.{methodSymbol.Name}()");
                }
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

    private static void ReportDiagnosticForInvocation(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        string accessedMember)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.DateTimeNow,
            invocation.GetLocation(),
            accessedMember);

        context.ReportDiagnostic(diagnostic);
    }
}
