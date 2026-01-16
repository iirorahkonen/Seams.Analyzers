using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.GlobalState;

/// <summary>
/// Analyzer that detects static mutable fields that create shared state.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StaticMutableFieldAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.StaticMutableField);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        // Check if the field is static
        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        // Skip readonly and const fields (they're immutable)
        if (fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) ||
            fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            return;

        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) as IFieldSymbol;
            if (fieldSymbol == null)
                continue;

            // Skip if field is part of a test class
            if (IsInTestClass(fieldSymbol))
                continue;

            // Check if the field type is mutable
            if (!IsMutableType(fieldSymbol.Type))
                continue;

            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.StaticMutableField,
                variable.Identifier.GetLocation(),
                fieldSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsMutableType(ITypeSymbol type)
    {
        // Value types that are simple (int, bool, etc.) are typically okay when static
        // The issue is with reference types that can be mutated
        if (type.IsValueType)
        {
            // But collections are problematic even if they're value types
            var typeName = type.ToDisplayString();
            return typeName.Contains("Dictionary") ||
                   typeName.Contains("List") ||
                   typeName.Contains("HashSet") ||
                   typeName.Contains("Collection");
        }

        // string is immutable, so it's okay
        if (type.SpecialType == SpecialType.System_String)
            return false;

        // Delegate types are mutable (can be reassigned)
        if (type.TypeKind == TypeKind.Delegate)
            return true;

        // Arrays are always mutable
        if (type is IArrayTypeSymbol)
            return true;

        // Check for immutable collections (they're okay)
        var fullTypeName = type.ToDisplayString();
        if (fullTypeName.StartsWith("System.Collections.Immutable.", System.StringComparison.Ordinal))
            return false;

        // Check for common mutable collection types
        if (IsCollectionType(type))
            return true;

        // Reference types are generally mutable
        if (type.IsReferenceType)
            return true;

        return false;
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        var fullName = type.ToDisplayString();

        return fullName.StartsWith("System.Collections.", System.StringComparison.Ordinal) ||
               type.Name is "List" or "Dictionary" or "HashSet" or "Queue" or "Stack" or
                          "LinkedList" or "SortedList" or "SortedDictionary" or "SortedSet" or
                          "ConcurrentDictionary" or "ConcurrentQueue" or "ConcurrentStack" or
                          "ConcurrentBag" or "BlockingCollection" or "ObservableCollection";
    }

    private static bool IsInTestClass(IFieldSymbol fieldSymbol)
    {
        var containingType = fieldSymbol.ContainingType;
        if (containingType == null)
            return false;

        // Check for common test framework attributes
        foreach (var attribute in containingType.GetAttributes())
        {
            var attrName = attribute.AttributeClass?.Name;
            if (attrName is "TestClass" or "TestFixture" or "Fact" or "Theory")
                return true;
        }

        // Check class name
        var className = containingType.Name;
        return className.EndsWith("Tests", System.StringComparison.Ordinal) ||
               className.EndsWith("Test", System.StringComparison.Ordinal) ||
               className.StartsWith("Test", System.StringComparison.Ordinal);
    }
}
