using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.GlobalState;

/// <summary>
/// Analyzer that detects the singleton pattern which creates global state.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingletonPatternAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.SingletonPattern);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
        if (classSymbol == null)
            return;

        // Check if this class follows the singleton pattern
        if (!IsSingletonPattern(classDeclaration, classSymbol, context.SemanticModel))
            return;

        // Check excluded types
        var excludedTypes = AnalyzerConfigOptions.GetExcludedTypes(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.SingletonPattern);

        if (AnalyzerConfigOptions.IsTypeExcluded(classSymbol, excludedTypes))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.SingletonPattern,
            classDeclaration.Identifier.GetLocation(),
            classSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsSingletonPattern(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol,
        SemanticModel semanticModel)
    {
        // Look for the classic singleton indicators:
        // 1. A static property or field that returns an instance of the class
        // 2. A private constructor

        var hasPrivateConstructor = false;
        var hasStaticInstanceMember = false;

        foreach (var member in classDeclaration.Members)
        {
            // Check for private constructor
            if (member is ConstructorDeclarationSyntax constructor)
            {
                if (constructor.Modifiers.Any(SyntaxKind.PrivateKeyword) ||
                    (!constructor.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     !constructor.Modifiers.Any(SyntaxKind.ProtectedKeyword) &&
                     !constructor.Modifiers.Any(SyntaxKind.InternalKeyword)))
                {
                    hasPrivateConstructor = true;
                }
            }

            // Check for static instance property
            if (member is PropertyDeclarationSyntax property)
            {
                if (property.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    var propertySymbol = semanticModel.GetDeclaredSymbol(property);
                    if (propertySymbol != null &&
                        SymbolEqualityComparer.Default.Equals(propertySymbol.Type, classSymbol))
                    {
                        // Common singleton property names
                        var name = property.Identifier.Text;
                        if (name is "Instance" or "Current" or "Default" or "Singleton")
                        {
                            hasStaticInstanceMember = true;
                        }
                    }
                }
            }

            // Check for static instance field
            if (member is FieldDeclarationSyntax field)
            {
                if (field.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                        if (fieldSymbol != null &&
                            SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, classSymbol))
                        {
                            // Check for common singleton field names or if the field initializes to new ClassName()
                            var name = variable.Identifier.Text;
                            if (name is "_instance" or "instance" or "_current" or "s_instance" or "Instance")
                            {
                                hasStaticInstanceMember = true;
                            }
                            else if (variable.Initializer?.Value is ObjectCreationExpressionSyntax creation)
                            {
                                var typeInfo = semanticModel.GetTypeInfo(creation);
                                if (SymbolEqualityComparer.Default.Equals(typeInfo.Type, classSymbol))
                                {
                                    hasStaticInstanceMember = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return hasPrivateConstructor && hasStaticInstanceMember;
    }
}
