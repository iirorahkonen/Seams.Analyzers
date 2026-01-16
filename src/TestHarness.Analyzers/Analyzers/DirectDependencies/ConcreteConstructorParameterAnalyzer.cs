using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.DirectDependencies;

/// <summary>
/// Analyzer that detects constructor parameters that use concrete types instead of abstractions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConcreteConstructorParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ConcreteConstructorParameter);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
    }

    private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;

        if (constructor.ParameterList.Parameters.Count == 0)
            return;

        var excludedTypes = AnalyzerConfigOptions.GetExcludedTypes(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.ConcreteConstructorParameter);

        foreach (var parameter in constructor.ParameterList.Parameters)
        {
            AnalyzeParameter(context, parameter, excludedTypes);
        }
    }

    private static void AnalyzeParameter(
        SyntaxNodeAnalysisContext context,
        ParameterSyntax parameter,
        ImmutableHashSet<string> excludedTypes)
    {
        if (parameter.Type == null)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(parameter.Type, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol namedType)
            return;

        // Check if type is excluded
        if (AnalyzerConfigOptions.IsTypeExcluded(namedType, excludedTypes))
            return;

        // Skip if the type is an interface or abstract class
        if (namedType.TypeKind == TypeKind.Interface || namedType.IsAbstract)
            return;

        // Skip value types, strings, and primitives
        if (namedType.IsValueType || namedType.SpecialType != SpecialType.None)
            return;

        // Skip common allowed types
        if (ShouldSkipType(namedType))
            return;

        // Skip delegates and Func/Action types
        if (namedType.TypeKind == TypeKind.Delegate ||
            namedType.Name.StartsWith("Func", System.StringComparison.Ordinal) ||
            namedType.Name.StartsWith("Action", System.StringComparison.Ordinal))
            return;

        // Check if the type is a concrete class that might benefit from abstraction
        if (namedType.TypeKind == TypeKind.Class && !namedType.IsSealed)
        {
            // Only flag if the type has interfaces or could reasonably be abstracted
            if (HasServiceCharacteristics(namedType))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ConcreteConstructorParameter,
                    parameter.GetLocation(),
                    parameter.Identifier.Text,
                    namedType.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool ShouldSkipType(INamedTypeSymbol type)
    {
        var fullName = type.ToDisplayString();

        // Skip collection types
        if (fullName.StartsWith("System.Collections.", System.StringComparison.Ordinal))
            return true;

        // Skip common framework types
        return fullName switch
        {
            "System.String" => true,
            "System.Object" => true,
            "System.Uri" => true,
            "System.Type" => true,
            "System.Text.StringBuilder" => true,
            "System.Threading.CancellationToken" => true,
            "Microsoft.Extensions.Logging.ILogger" => true,
            _ when fullName.StartsWith("Microsoft.Extensions.Logging.ILogger<", System.StringComparison.Ordinal) => true,
            _ when fullName.StartsWith("Microsoft.Extensions.Options.IOptions<", System.StringComparison.Ordinal) => true,
            _ when fullName.StartsWith("Microsoft.Extensions.Options.IOptionsSnapshot<", System.StringComparison.Ordinal) => true,
            _ when fullName.StartsWith("Microsoft.Extensions.Options.IOptionsMonitor<", System.StringComparison.Ordinal) => true,
            _ => false
        };
    }

    private static bool HasServiceCharacteristics(INamedTypeSymbol type)
    {
        // Check if the type name suggests it's a service
        var name = type.Name;
        if (name.EndsWith("Service", System.StringComparison.Ordinal) ||
            name.EndsWith("Repository", System.StringComparison.Ordinal) ||
            name.EndsWith("Provider", System.StringComparison.Ordinal) ||
            name.EndsWith("Factory", System.StringComparison.Ordinal) ||
            name.EndsWith("Manager", System.StringComparison.Ordinal) ||
            name.EndsWith("Handler", System.StringComparison.Ordinal) ||
            name.EndsWith("Client", System.StringComparison.Ordinal) ||
            name.EndsWith("Gateway", System.StringComparison.Ordinal) ||
            name.EndsWith("Adapter", System.StringComparison.Ordinal))
        {
            return true;
        }

        // Check if the type implements any interfaces (suggests abstraction is possible)
        if (type.Interfaces.Length > 0)
        {
            // Filter out common marker interfaces
            foreach (var iface in type.Interfaces)
            {
                var ifaceName = iface.ToDisplayString();
                if (!ifaceName.StartsWith("System.IDisposable", System.StringComparison.Ordinal) &&
                    !ifaceName.StartsWith("System.IComparable", System.StringComparison.Ordinal) &&
                    !ifaceName.StartsWith("System.IEquatable", System.StringComparison.Ordinal) &&
                    !ifaceName.StartsWith("System.IFormattable", System.StringComparison.Ordinal) &&
                    !ifaceName.StartsWith("System.ICloneable", System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
