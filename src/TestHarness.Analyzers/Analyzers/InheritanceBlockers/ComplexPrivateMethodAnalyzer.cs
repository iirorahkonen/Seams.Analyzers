using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.InheritanceBlockers;

/// <summary>
/// Analyzer that detects complex private methods that should be extracted to separate testable classes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComplexPrivateMethodAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultLineThreshold = 50;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ComplexPrivateMethod);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Only check private methods
        var hasPrivate = methodDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword);
        var hasNoAccessModifier = !methodDeclaration.Modifiers.Any(m =>
            m.IsKind(SyntaxKind.PublicKeyword) ||
            m.IsKind(SyntaxKind.ProtectedKeyword) ||
            m.IsKind(SyntaxKind.InternalKeyword) ||
            m.IsKind(SyntaxKind.PrivateKeyword));

        // Methods without access modifiers are private by default
        if (!hasPrivate && !hasNoAccessModifier)
            return;

        // Skip methods without a body
        if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
            return;

        // Get configured threshold
        var threshold = AnalyzerConfigOptions.GetComplexityThreshold(
            context.Options,
            context.Node.SyntaxTree,
            DefaultLineThreshold);

        // Calculate method line count
        var lineCount = CalculateLineCount(methodDeclaration);

        if (lineCount < threshold)
            return;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
        if (methodSymbol == null)
            return;

        // Check excluded methods
        var excludedMethods = AnalyzerConfigOptions.GetExcludedMethods(
            context.Options,
            context.Node.SyntaxTree,
            DiagnosticIds.ComplexPrivateMethod);

        if (AnalyzerConfigOptions.IsMethodExcluded(methodSymbol, excludedMethods))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.ComplexPrivateMethod,
            methodDeclaration.Identifier.GetLocation(),
            methodSymbol.Name,
            lineCount);

        context.ReportDiagnostic(diagnostic);
    }

    private static int CalculateLineCount(MethodDeclarationSyntax method)
    {
        var span = method.Span;
        var text = method.SyntaxTree.GetText();

        var startLine = text.Lines.GetLineFromPosition(span.Start).LineNumber;
        var endLine = text.Lines.GetLineFromPosition(span.End).LineNumber;

        return endLine - startLine + 1;
    }
}
