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
        if (IsInCompositionRoot(context.Node))
            return false;

        // Check if it's a factory delegate or Func<IServiceProvider, T> pattern
        if (IsInFactoryDelegate(context.Node))
            return false;

        return true;
    }

    private static bool IsInCompositionRoot(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ClassDeclarationSyntax classDecl)
            {
                var className = classDecl.Identifier.Text;
                if (className is "Startup" or "Program" or "CompositionRoot" or
                    "ServiceCollectionExtensions" or "DependencyInjectionExtensions")
                {
                    return true;
                }
            }
            else if (current is MethodDeclarationSyntax methodDecl)
            {
                var methodName = methodDecl.Identifier.Text;
                if (methodName is "ConfigureServices" or "AddServices" or "RegisterServices" or
                    "Configure" or "ConfigureContainer")
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsInFactoryDelegate(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            // Check if we're inside a lambda or anonymous method
            if (current is LambdaExpressionSyntax || current is AnonymousMethodExpressionSyntax)
            {
                // Check if the lambda is an argument to AddSingleton, AddScoped, etc.
                if (current.Parent is ArgumentSyntax arg && arg.Parent is ArgumentListSyntax argList)
                {
                    if (argList.Parent is InvocationExpressionSyntax invocation &&
                        invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        var methodName = memberAccess.Name.Identifier.Text;
                        if (methodName.StartsWith("Add", System.StringComparison.Ordinal) ||
                            methodName is "Register" or "RegisterType" or "RegisterInstance")
                        {
                            return true;
                        }
                    }
                }
            }

            current = current.Parent;
        }

        return false;
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.ServiceLocator,
            invocation.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}
