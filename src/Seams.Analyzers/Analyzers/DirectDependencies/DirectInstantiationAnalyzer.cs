using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.DirectDependencies;

/// <summary>
/// Analyzer that detects direct instantiation of concrete types using 'new' keyword.
/// This pattern creates hard dependencies that prevent seam injection for testing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectInstantiationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DirectInstantiation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
        AnalyzeCreation(context, objectCreation, objectCreation.Type);
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var implicitCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(implicitCreation, context.CancellationToken);

        if (typeInfo.Type is INamedTypeSymbol namedType)
        {
            AnalyzeTypeSymbol(context, implicitCreation, namedType);
        }
    }

    private static void AnalyzeCreation(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax creationExpression,
        TypeSyntax? typeSyntax)
    {
        if (typeSyntax == null)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return;

        AnalyzeTypeSymbol(context, creationExpression, namedType);
    }

    private static void AnalyzeTypeSymbol(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax creationExpression,
        INamedTypeSymbol namedType)
    {
        // Skip if in excluded types
        var excludedTypes = AnalyzerConfigOptions.GetExcludedTypes(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.DirectInstantiation);

        if (AnalyzerConfigOptions.IsTypeExcluded(namedType, excludedTypes))
            return;

        // Skip value types, primitives, and framework types
        if (ShouldSkipType(namedType))
            return;

        // Skip if the creation is in a field initializer (static readonly pattern)
        if (IsInFieldInitializer(creationExpression))
            return;

        // Skip if the type is defined in the same project (local types)
        if (IsLocalType(namedType, context))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.DirectInstantiation,
            creationExpression.GetLocation(),
            namedType.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool ShouldSkipType(INamedTypeSymbol type)
    {
        // Skip value types
        if (type.IsValueType)
            return true;

        // Skip collection types (List, Dictionary, etc.)
        if (IsCollectionType(type))
            return true;

        // Skip common framework types that are typically okay to instantiate
        if (IsAllowedFrameworkType(type))
            return true;

        // Skip types with no methods besides Object methods (data classes)
        if (IsDataClass(type))
            return true;

        // Skip anonymous types
        if (type.IsAnonymousType)
            return true;

        return false;
    }

    private static bool IsCollectionType(INamedTypeSymbol type)
    {
        var fullName = type.ToDisplayString();

        // Common collection namespaces/types
        return fullName.StartsWith("System.Collections.", System.StringComparison.Ordinal) ||
               type.Name is "List" or "Dictionary" or "HashSet" or "Queue" or "Stack" or
                          "LinkedList" or "SortedList" or "SortedDictionary" or "SortedSet" or
                          "ConcurrentDictionary" or "ConcurrentQueue" or "ConcurrentStack" or
                          "ConcurrentBag" or "BlockingCollection" or "ObservableCollection";
    }

    private static bool IsAllowedFrameworkType(INamedTypeSymbol type)
    {
        var fullName = type.ToDisplayString();

        // StringBuilder, Exception types, etc. are okay
        return fullName == "System.Text.StringBuilder" ||
               fullName == "System.Text.RegularExpressions.Regex" ||
               fullName.EndsWith("Exception", System.StringComparison.Ordinal) ||
               fullName == "System.Uri" ||
               fullName == "System.UriBuilder" ||
               fullName == "System.Random" ||
               fullName == "System.Lazy`1" ||
               fullName.StartsWith("System.Tuple", System.StringComparison.Ordinal) ||
               fullName.StartsWith("System.ValueTuple", System.StringComparison.Ordinal);
    }

    private static bool IsDataClass(INamedTypeSymbol type)
    {
        // Check if the type has only properties (data class/DTO pattern)
        var members = type.GetMembers();
        var hasNonPropertyMethods = false;

        foreach (var member in members)
        {
            if (member is IMethodSymbol method)
            {
                // Skip constructors, property accessors, and Object methods
                if (method.MethodKind == MethodKind.Constructor ||
                    method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet ||
                    method.MethodKind == MethodKind.StaticConstructor ||
                    IsObjectMethod(method))
                {
                    continue;
                }

                hasNonPropertyMethods = true;
                break;
            }
        }

        return !hasNonPropertyMethods;
    }

    private static bool IsObjectMethod(IMethodSymbol method)
    {
        return method.Name is "ToString" or "GetHashCode" or "Equals" or "GetType" or "Finalize" or "MemberwiseClone";
    }

    private static bool IsInFieldInitializer(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is FieldDeclarationSyntax)
                return true;
            if (current is MethodDeclarationSyntax ||
                current is ConstructorDeclarationSyntax ||
                current is PropertyDeclarationSyntax)
                return false;
            current = current.Parent;
        }
        return false;
    }

    private static bool IsLocalType(INamedTypeSymbol type, SyntaxNodeAnalysisContext context)
    {
        // Skip types from the same assembly
        var compilation = context.Compilation;
        return SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly);
    }
}
