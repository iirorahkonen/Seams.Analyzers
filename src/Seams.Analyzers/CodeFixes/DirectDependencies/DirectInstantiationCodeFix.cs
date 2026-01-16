using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Seams.Analyzers.CodeFixes.DirectDependencies;

/// <summary>
/// Code fix provider that extracts direct instantiation to a constructor parameter.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DirectInstantiationCodeFix))]
[Shared]
public sealed class DirectInstantiationCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DirectInstantiation);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the object creation expression
        var node = root.FindNode(diagnosticSpan);
        var objectCreation = node.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
        if (objectCreation == null)
            return;

        // Get the type being created
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return;

        var typeInfo = semanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        if (typeInfo.Type == null)
            return;

        var typeName = typeInfo.Type.Name;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract '{typeName}' to constructor parameter",
                createChangedDocument: c => ExtractToConstructorParameterAsync(context.Document, objectCreation, typeInfo.Type, c),
                equivalenceKey: "ExtractToConstructorParameter"),
            diagnostic);
    }

    private static async Task<Document> ExtractToConstructorParameterAsync(
        Document document,
        ObjectCreationExpressionSyntax objectCreation,
        ITypeSymbol type,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Find the containing class
        var containingClass = objectCreation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
            return document;

        // Generate field name from type name
        var fieldName = "_" + char.ToLowerInvariant(type.Name[0]) + type.Name.Substring(1);
        var parameterName = char.ToLowerInvariant(type.Name[0]) + type.Name.Substring(1);

        // Create the field declaration
        var fieldDeclaration = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(fieldName)))))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

        // Find or create constructor
        var existingConstructor = containingClass.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword));

        if (existingConstructor != null)
        {
            // Add parameter to existing constructor
            var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                .WithType(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

            var newParameterList = existingConstructor.ParameterList.AddParameters(newParameter);

            // Add assignment statement
            var assignmentStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName(parameterName)));

            var newBody = existingConstructor.Body?.AddStatements(assignmentStatement)
                ?? SyntaxFactory.Block(assignmentStatement);

            var newConstructor = existingConstructor
                .WithParameterList(newParameterList)
                .WithBody(newBody);

            editor.ReplaceNode(existingConstructor, newConstructor);
        }
        else
        {
            // Create new constructor
            var constructorParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                .WithType(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

            var assignmentStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName(parameterName)));

            var newConstructor = SyntaxFactory.ConstructorDeclaration(containingClass.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(constructorParameter)))
                .WithBody(SyntaxFactory.Block(assignmentStatement));

            editor.InsertMembers(containingClass, 0, new SyntaxNode[] { fieldDeclaration, newConstructor });
        }

        // Replace the object creation with field reference
        editor.ReplaceNode(objectCreation, SyntaxFactory.IdentifierName(fieldName));

        // Add field if constructor existed
        if (existingConstructor != null)
        {
            editor.InsertBefore(existingConstructor, fieldDeclaration);
        }

        return editor.GetChangedDocument();
    }
}
