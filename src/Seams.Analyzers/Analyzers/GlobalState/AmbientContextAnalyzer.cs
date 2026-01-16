using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.GlobalState;

/// <summary>
/// Analyzer that detects usage of ambient context patterns like HttpContext.Current.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AmbientContextAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> AmbientContexts =
        ImmutableDictionary.CreateRange(System.StringComparer.Ordinal, new[]
        {
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Web.HttpContext",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "Current")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Threading.Thread",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "CurrentThread", "CurrentPrincipal")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Security.Claims.ClaimsPrincipal",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "Current")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Threading.SynchronizationContext",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "Current")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Runtime.Remoting.Messaging.CallContext",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "LogicalGetData", "LogicalSetData", "GetData", "SetData")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Transactions.Transaction",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "Current")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.OperationContext",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "Current"))
        });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.AmbientContext);

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
        var memberName = propertySymbol.Name;

        // Check against known ambient contexts
        if (AmbientContexts.TryGetValue(fullTypeName, out var members))
        {
            if (members.Contains(memberName))
            {
                ReportDiagnostic(context, memberAccess, $"{containingType.Name}.{memberName}");
            }
        }
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
        var methodName = methodSymbol.Name;

        // Check against known ambient context methods (like CallContext.GetData)
        if (AmbientContexts.TryGetValue(fullTypeName, out var members))
        {
            if (members.Contains(methodName))
            {
                ReportDiagnostic(context, invocation, $"{containingType.Name}.{methodName}");
            }
        }
    }

    private static void ReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        SyntaxNode node,
        string contextName)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.AmbientContext,
            node.GetLocation(),
            contextName);

        context.ReportDiagnostic(diagnostic);
    }
}
