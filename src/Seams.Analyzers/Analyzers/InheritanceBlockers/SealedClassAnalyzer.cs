using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.InheritanceBlockers;

/// <summary>
/// Analyzer that detects sealed classes that prevent inheritance-based seams.
/// Only flags public classes that could benefit from being unsealed for testing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SealedClassAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.SealedClass);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if the class is sealed
        if (!classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword))
            return;

        // Only flag public or protected classes
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) &&
            !classDeclaration.Modifiers.Any(SyntaxKind.ProtectedKeyword))
            return;

        // Get the semantic model symbol
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
        if (symbol == null)
            return;

        // Check excluded namespaces
        var excludedNamespaces = AnalyzerConfigOptions.GetExcludedNamespaces(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.SealedClass);

        if (AnalyzerConfigOptions.IsInExcludedNamespace(symbol, excludedNamespaces))
            return;

        // Check excluded types
        var excludedTypes = AnalyzerConfigOptions.GetExcludedTypes(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.SealedClass);

        if (AnalyzerConfigOptions.IsTypeExcluded(symbol, excludedTypes))
            return;

        // Skip records (they are sealed by default and unsealing changes semantics)
        if (classDeclaration.Keyword.IsKind(SyntaxKind.RecordKeyword))
            return;

        // Skip if the class has no instance methods (just data)
        if (!HasInstanceMethods(symbol))
            return;

        // Skip certain naming patterns that typically should remain sealed
        if (ShouldSkipBasedOnNaming(symbol.Name))
            return;

        var sealedToken = classDeclaration.Modifiers.First(m => m.IsKind(SyntaxKind.SealedKeyword));
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.SealedClass,
            sealedToken.GetLocation(),
            symbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool HasInstanceMethods(INamedTypeSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            if (member is IMethodSymbol method &&
                !method.IsStatic &&
                method.MethodKind == MethodKind.Ordinary &&
                method.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected)
            {
                return true;
            }
        }
        return false;
    }

    private static bool ShouldSkipBasedOnNaming(string className)
    {
        // Exception types should typically remain sealed
        if (className.EndsWith("Exception", System.StringComparison.Ordinal))
            return true;

        // Attribute types should typically remain sealed
        if (className.EndsWith("Attribute", System.StringComparison.Ordinal))
            return true;

        // EventArgs types should typically remain sealed
        if (className.EndsWith("EventArgs", System.StringComparison.Ordinal))
            return true;

        return false;
    }
}
