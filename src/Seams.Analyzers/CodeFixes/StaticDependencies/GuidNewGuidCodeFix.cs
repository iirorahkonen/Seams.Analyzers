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

namespace Seams.Analyzers.CodeFixes.StaticDependencies;

/// <summary>
/// Code fix provider that replaces Guid.NewGuid() with IGuidGenerator injection.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GuidNewGuidCodeFix))]
[Shared]
public sealed class GuidNewGuidCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.GuidNewGuid);

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
                title: "Inject IGuidGenerator",
                createChangedDocument: c => InjectGuidGeneratorAsync(context.Document, invocation, c),
                equivalenceKey: "InjectGuidGenerator"),
            diagnostic);
    }

    private static async Task<Document> InjectGuidGeneratorAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Find the containing class
        var containingClass = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
            return document;

        const string fieldName = "_guidGenerator";
        const string parameterName = "guidGenerator";
        const string interfaceName = "IGuidGenerator";

        // Check if field already exists
        var fieldExists = containingClass.Members
            .OfType<FieldDeclarationSyntax>()
            .Any(f => f.Declaration.Variables.Any(v => v.Identifier.Text == fieldName));

        var newClass = containingClass;

        if (!fieldExists)
        {
            // Create the field declaration
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(interfaceName))
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
                var paramExists = existingConstructor.ParameterList.Parameters
                    .Any(p => p.Identifier.Text == parameterName);

                if (!paramExists)
                {
                    var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                        .WithType(SyntaxFactory.ParseTypeName(interfaceName));

                    var newParameterList = existingConstructor.ParameterList.AddParameters(newParameter);

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
            }
            else
            {
                var constructorParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                    .WithType(SyntaxFactory.ParseTypeName(interfaceName));

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
        }

        // Replace Guid.NewGuid() with _guidGenerator.NewGuid()
        var replacement = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(fieldName),
                SyntaxFactory.IdentifierName("NewGuid")));

        var oldInvocationInNewClass = newClass.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(i => i.ToString() == invocation.ToString());

        if (oldInvocationInNewClass != null)
        {
            newClass = newClass.ReplaceNode(oldInvocationInNewClass, replacement);
        }

        var newRoot = root.ReplaceNode(containingClass, newClass);
        return document.WithSyntaxRoot(newRoot);
    }
}
