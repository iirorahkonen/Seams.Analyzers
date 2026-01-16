using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.StaticDependencies;

/// <summary>
/// Analyzer that detects access to static properties that create untestable dependencies.
/// Focuses on ConfigurationManager and similar ambient context properties.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StaticPropertyAccessAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> ProblematicProperties =
        ImmutableDictionary.CreateRange(System.StringComparer.Ordinal, new[]
        {
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Configuration.ConfigurationManager",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "AppSettings", "ConnectionStrings")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Environment",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "CurrentDirectory", "MachineName", "UserName", "OSVersion")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Threading.Thread",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "CurrentThread", "CurrentPrincipal")),
            new System.Collections.Generic.KeyValuePair<string, ImmutableHashSet<string>>(
                "System.Globalization.CultureInfo",
                ImmutableHashSet.Create(System.StringComparer.Ordinal, "CurrentCulture", "CurrentUICulture"))
        });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.StaticPropertyAccess);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
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

        // Check if this is a problematic static property
        if (ProblematicProperties.TryGetValue(fullTypeName, out var properties))
        {
            if (properties.Contains(propertyName))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.StaticPropertyAccess,
                    memberAccess.GetLocation(),
                    containingType.Name,
                    propertyName);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
