using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.Infrastructure;

/// <summary>
/// Analyzer that detects direct HttpClient creation.
/// Creating HttpClient directly can lead to socket exhaustion and makes testing difficult.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HttpClientCreationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.HttpClientCreation);

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

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        AnalyzeType(context, objectCreation, typeInfo.Type);
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var implicitCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(implicitCreation, context.CancellationToken);
        AnalyzeType(context, implicitCreation, typeInfo.Type);
    }

    private static void AnalyzeType(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax creationExpression,
        ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedType)
            return;

        var fullTypeName = namedType.ToDisplayString();

        // Check for HttpClient instantiation
        if (fullTypeName != "System.Net.Http.HttpClient")
            return;

        // Skip if in a factory method or HttpClientFactory context
        if (IsInHttpClientFactoryContext(creationExpression))
            return;

        // Skip if it's a static readonly field (sometimes acceptable for simple cases)
        if (IsStaticReadonlyFieldInitializer(creationExpression))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.HttpClientCreation,
            creationExpression.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsInHttpClientFactoryContext(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            // Check if we're in a class that implements IHttpClientFactory
            if (current is ClassDeclarationSyntax classDecl)
            {
                var className = classDecl.Identifier.Text;
                if (className.Contains("Factory") || className.Contains("HttpClient"))
                {
                    return true;
                }
            }

            // Check if we're in a method that creates HttpClient for factory purposes
            if (current is MethodDeclarationSyntax methodDecl)
            {
                var methodName = methodDecl.Identifier.Text;
                if (methodName is "CreateClient" or "CreateHttpClient" or "Build")
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsStaticReadonlyFieldInitializer(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is FieldDeclarationSyntax field)
            {
                return field.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                       field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
            }

            if (current is MethodDeclarationSyntax ||
                current is ConstructorDeclarationSyntax ||
                current is PropertyDeclarationSyntax)
            {
                return false;
            }

            current = current.Parent;
        }

        return false;
    }
}
