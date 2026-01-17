using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Seams.Analyzers.Analyzers.Complexity;

/// <summary>
/// Analyzer that detects methods with high cyclomatic complexity.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HighCyclomaticComplexityAnalyzer : DiagnosticAnalyzer
{
    private const int DefaultThreshold = 25;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.HighCyclomaticComplexity);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        context.RegisterSyntaxNodeAction(AnalyzeAccessor, SyntaxKind.GetAccessorDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeAccessor, SyntaxKind.SetAccessorDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Skip abstract and interface methods (no body)
        if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
            return;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
        if (methodSymbol == null)
            return;

        // Skip abstract/extern methods
        if (methodSymbol.IsAbstract || methodSymbol.IsExtern)
            return;

        var threshold = AnalyzerConfigOptions.GetCyclomaticComplexityThreshold(
            context.Options,
            context.Node.SyntaxTree,
            DefaultThreshold);

        var complexity = CalculateComplexity(methodDeclaration);

        if (complexity > threshold)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.HighCyclomaticComplexity,
                methodDeclaration.Identifier.GetLocation(),
                methodSymbol.Name,
                complexity,
                threshold);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
    {
        var localFunction = (LocalFunctionStatementSyntax)context.Node;

        if (localFunction.Body == null && localFunction.ExpressionBody == null)
            return;

        var threshold = AnalyzerConfigOptions.GetCyclomaticComplexityThreshold(
            context.Options,
            context.Node.SyntaxTree,
            DefaultThreshold);

        var complexity = CalculateComplexity(localFunction);

        if (complexity > threshold)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.HighCyclomaticComplexity,
                localFunction.Identifier.GetLocation(),
                localFunction.Identifier.Text,
                complexity,
                threshold);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeAccessor(SyntaxNodeAnalysisContext context)
    {
        var accessor = (AccessorDeclarationSyntax)context.Node;

        // Skip accessors without a body
        if (accessor.Body == null && accessor.ExpressionBody == null)
            return;

        var threshold = AnalyzerConfigOptions.GetCyclomaticComplexityThreshold(
            context.Options,
            context.Node.SyntaxTree,
            DefaultThreshold);

        var complexity = CalculateComplexity(accessor);

        if (complexity > threshold)
        {
            // Get the property name for the diagnostic message
            var propertyDeclaration = accessor.Parent?.Parent as PropertyDeclarationSyntax;
            var accessorType = accessor.IsKind(SyntaxKind.GetAccessorDeclaration) ? "get" : "set";
            var name = propertyDeclaration != null
                ? $"{propertyDeclaration.Identifier.Text}.{accessorType}"
                : accessorType;

            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.HighCyclomaticComplexity,
                accessor.Keyword.GetLocation(),
                name,
                complexity,
                threshold);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static int CalculateComplexity(SyntaxNode node)
    {
        var walker = new ComplexityWalker();
        walker.Visit(node);
        return walker.Complexity;
    }

    /// <summary>
    /// Walks the syntax tree and counts decision points for cyclomatic complexity.
    /// Lambdas and local functions are excluded (they are analyzed separately).
    /// </summary>
    private sealed class ComplexityWalker : CSharpSyntaxWalker
    {
        // Start with 1 as base complexity (one path through the method)
        public int Complexity { get; private set; } = 1;

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            Complexity++;
            base.VisitIfStatement(node);
        }

        public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            Complexity++;
            base.VisitCaseSwitchLabel(node);
        }

        public override void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
        {
            Complexity++;
            base.VisitCasePatternSwitchLabel(node);
        }

        public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            // Don't count discard patterns (_)
            if (node.Pattern is not DiscardPatternSyntax)
            {
                Complexity++;
            }
            base.VisitSwitchExpressionArm(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            Complexity++;
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            Complexity++;
            base.VisitForEachStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            Complexity++;
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Complexity++;
            base.VisitDoStatement(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            Complexity++;
            base.VisitCatchClause(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // Count &&, ||, and ?? operators
            if (node.IsKind(SyntaxKind.LogicalAndExpression) ||
                node.IsKind(SyntaxKind.LogicalOrExpression) ||
                node.IsKind(SyntaxKind.CoalesceExpression))
            {
                Complexity++;
            }
            base.VisitBinaryExpression(node);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            // Ternary operator ?:
            Complexity++;
            base.VisitConditionalExpression(node);
        }

        // Don't descend into lambdas - they are separate code units
        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            // Don't call base - skip lambda body
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            // Don't call base - skip lambda body
        }

        // Don't descend into local functions - they are analyzed separately
        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            // Don't call base - skip local function body
        }

        // Don't descend into anonymous methods
        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            // Don't call base - skip anonymous method body
        }
    }
}
