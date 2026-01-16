using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.DirectDependencies;

/// <summary>
/// Analyzer that detects usage of the Service Locator anti-pattern.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServiceLocatorAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ServiceLocatorPatterns = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        "ServiceLocator",
        "DependencyResolver",
        "CommonServiceLocator",
        "IServiceLocator");

    private static readonly ImmutableHashSet<string> ServiceLocatorMethods = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        "Resolve",
        "GetService",
        "GetInstance",
        "GetRequiredService",
        "ResolveOptional",
        "TryResolve");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ServiceLocator);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check for patterns like:
        // ServiceLocator.Resolve<T>()
        // container.Resolve<T>()
        // serviceProvider.GetService<T>() - outside of composition root

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            AnalyzeMemberAccess(context, invocation, memberAccess);
        }
    }

    private static void AnalyzeMemberAccess(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        MemberAccessExpressionSyntax memberAccess)
    {
        var methodName = memberAccess.Name.Identifier.Text;

        // Check if the method name suggests service location
        if (!ServiceLocatorMethods.Contains(methodName))
            return;

        // Get the symbol for the method being called
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        // Check if it's a well-known service locator type
        var containingTypeName = methodSymbol.ContainingType?.Name;
        if (containingTypeName != null && ServiceLocatorPatterns.Contains(containingTypeName))
        {
            ReportDiagnostic(context, invocation);
            return;
        }

        // Check for IServiceProvider usage outside of composition root
        if (IsServiceProviderUsageOutsideCompositionRoot(context, memberAccess, methodSymbol))
        {
            ReportDiagnostic(context, invocation);
            return;
        }

        // Check for static service locator access
        if (memberAccess.Expression is IdentifierNameSyntax identifier)
        {
            var typeSymbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol;
            if (typeSymbol is INamedTypeSymbol namedType && ServiceLocatorPatterns.Contains(namedType.Name))
            {
                ReportDiagnostic(context, invocation);
            }
        }
    }

    private static bool IsServiceProviderUsageOutsideCompositionRoot(
        SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax memberAccess,
        IMethodSymbol methodSymbol)
    {
        // Check if method is from IServiceProvider
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return false;

        var fullTypeName = containingType.ToDisplayString();
        if (fullTypeName != "System.IServiceProvider" &&
            fullTypeName != "Microsoft.Extensions.DependencyInjection.IServiceProvider" &&
            !fullTypeName.Contains("ServiceProvider"))
        {
            return false;
        }

        // Check if we're in a composition root context (Startup, Program, ConfigureServices, etc.)
        if (context.Node.IsInCompositionRoot())
            return false;

        // Check if it's a factory delegate or Func<IServiceProvider, T> pattern
        if (context.Node.IsInFactoryDelegate())
            return false;

        return true;
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.ServiceLocator,
            invocation.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}
