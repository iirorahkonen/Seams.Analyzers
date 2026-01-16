using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.InheritanceBlockers;

/// <summary>
/// Analyzer that detects non-virtual public methods that prevent override-based seams.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonVirtualMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.NonVirtualMethod);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Only check public methods
        if (!methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
            return;

        // Skip if already virtual, override, or abstract
        if (methodDeclaration.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
            methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword) ||
            methodDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword))
            return;

        // Skip static methods
        if (methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        // Get the containing class
        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
            return;

        // Skip if the class is sealed (can't override anyway)
        if (classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword))
            return;

        // Skip if class is not public
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
            return;

        // Skip static classes
        if (classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
        if (methodSymbol == null)
            return;

        // Check excluded methods
        var excludedMethods = AnalyzerConfigOptions.GetExcludedMethods(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.NonVirtualMethod);

        if (AnalyzerConfigOptions.IsMethodExcluded(methodSymbol, excludedMethods))
            return;

        // Skip interface implementations (they follow a contract)
        if (IsInterfaceImplementation(methodSymbol))
            return;

        // Skip methods that override base class methods
        if (methodSymbol.IsOverride)
            return;

        // Skip common patterns that don't need to be virtual
        if (ShouldSkipMethod(methodSymbol))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.NonVirtualMethod,
            methodDeclaration.Identifier.GetLocation(),
            methodSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsInterfaceImplementation(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return false;

        foreach (var iface in containingType.AllInterfaces)
        {
            foreach (var member in iface.GetMembers())
            {
                if (member is IMethodSymbol interfaceMethod)
                {
                    var implementation = containingType.FindImplementationForInterfaceMember(interfaceMethod);
                    if (SymbolEqualityComparer.Default.Equals(implementation, methodSymbol))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool ShouldSkipMethod(IMethodSymbol methodSymbol)
    {
        var methodName = methodSymbol.Name;

        // Skip Dispose pattern methods
        if (methodName is "Dispose" or "DisposeAsync")
            return true;

        // Skip common framework patterns
        if (methodName is "ToString" or "GetHashCode" or "Equals" or "CompareTo" or "Clone")
            return true;

        // Skip methods with simple bodies (single expression)
        // This is a heuristic - simple getter-like methods typically don't need to be virtual
        if (methodSymbol.DeclaringSyntaxReferences.Length > 0)
        {
            var syntax = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax();
            if (syntax is MethodDeclarationSyntax methodSyntax)
            {
                // Expression body methods
                if (methodSyntax.ExpressionBody != null)
                    return true;

                // Methods with only a return statement
                if (methodSyntax.Body?.Statements.Count == 1 &&
                    methodSyntax.Body.Statements[0] is ReturnStatementSyntax)
                    return true;
            }
        }

        return false;
    }
}
