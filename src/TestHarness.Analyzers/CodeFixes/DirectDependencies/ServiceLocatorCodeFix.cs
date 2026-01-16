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

namespace TestHarness.Analyzers.CodeFixes.DirectDependencies;

/// <summary>
/// Code fix provider that converts service locator usage to constructor injection.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ServiceLocatorCodeFix))]
[Shared]
public sealed class ServiceLocatorCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.ServiceLocator);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Convert to constructor injection",
                createChangedDocument: c => ConvertToConstructorInjectionAsync(context.Document, invocation, c),
                equivalenceKey: "ConvertToConstructorInjection"),
            diagnostic);
    }

    private static async Task<Document> ConvertToConstructorInjectionAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        // Try to determine the resolved type from the generic argument
        ITypeSymbol? resolvedType = null;
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
        {
            var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
            if (typeArg != null)
            {
                var typeInfo = semanticModel.GetTypeInfo(typeArg, cancellationToken);
                resolvedType = typeInfo.Type;
            }
        }

        if (resolvedType == null)
            return document;

        // Find the containing class
        var containingClass = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
            return document;

        // Generate field name from type name
        var typeName = resolvedType.Name;
        if (typeName.StartsWith("I") && typeName.Length > 1 && char.IsUpper(typeName[1]))
        {
            typeName = typeName.Substring(1);
        }
        var fieldName = "_" + char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
        var parameterName = char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);

        // Create the field declaration
        var typeDisplayString = resolvedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var fieldDeclaration = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(typeDisplayString))
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

        ClassDeclarationSyntax newClass;

        if (existingConstructor != null)
        {
            // Add parameter to existing constructor
            var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                .WithType(SyntaxFactory.ParseTypeName(typeDisplayString));

            var newParameterList = existingConstructor.ParameterList.AddParameters(newParameter);

            // Add assignment statement at the beginning
            var assignmentStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName(parameterName)));

            var statements = existingConstructor.Body?.Statements.Insert(0, assignmentStatement)
                ?? SyntaxFactory.SingletonList<StatementSyntax>(assignmentStatement);

            var newConstructor = existingConstructor
                .WithParameterList(newParameterList)
                .WithBody(SyntaxFactory.Block(statements));

            newClass = containingClass
                .ReplaceNode(existingConstructor, newConstructor)
                .WithMembers(containingClass.Members.Insert(0, fieldDeclaration));
        }
        else
        {
            // Create new constructor
            var constructorParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                .WithType(SyntaxFactory.ParseTypeName(typeDisplayString));

            var assignmentStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName(parameterName)));

            var newConstructor = SyntaxFactory.ConstructorDeclaration(containingClass.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(constructorParameter)))
                .WithBody(SyntaxFactory.Block(assignmentStatement));

            newClass = containingClass.WithMembers(
                containingClass.Members.Insert(0, fieldDeclaration).Insert(1, newConstructor));
        }

        // Replace the service locator call with field reference
        var newInvocation = SyntaxFactory.IdentifierName(fieldName);

        // Find the invocation in the new class and replace it
        var oldInvocationInNewClass = newClass.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(i => i.ToString() == invocation.ToString());

        if (oldInvocationInNewClass != null)
        {
            newClass = newClass.ReplaceNode(oldInvocationInNewClass, newInvocation);
        }

        var newRoot = root.ReplaceNode(containingClass, newClass);
        return document.WithSyntaxRoot(newRoot);
    }
}
